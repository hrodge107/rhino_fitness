using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public class CalendarDayModel
    {
        public DateTime Date { get; set; }
        public string DayNumber => Date.Day.ToString();
        public bool IsSelected { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;
        public bool IsCurrentMonth { get; set; }
        public bool HasExercises { get; set; }
    }

    public class PlannedWorkoutItem
    {
        public int ScheduledExerciseId { get; set; }
        public string ExerciseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GifUrl { get; set; } = string.Empty;
        public string BodyPart { get; set; } = string.Empty;
        public string Muscle { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";

        public bool IsMissed => Status == "MISSED";
        public bool IsNotMissed => Status == "PENDING";
        public string CardBgColor => IsMissed ? "#3A2020" : "#2A2A2E";
    }

    public partial class PlannerViewModel : BaseViewModel
    {
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IPlannerStateService _plannerStateService;

        // Rev 2: cached 42-day range; populated by BuildCalendarAsync, consumed by LoadScheduledExercisesAsync.
        private List<ScheduledExercise> _cachedRangeData = new();

        // Rev 5: status string constants — single source of truth.
        private static class ExerciseStatus
        {
            public const string Pending = "PENDING";
            public const string Missed  = "MISSED";
        }

        // Rev 1: throws instead of silently loading data for user ID 1.
        private int UserId => _plannerStateService.CurrentUser?.Id
            ?? throw new InvalidOperationException("No authenticated user.");

        [ObservableProperty]
        private string _currentMonthYearText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentMonthName))]
        [NotifyPropertyChangedFor(nameof(CurrentYearText))]
        private DateTime _currentMonth;

        public string CurrentMonthName => CurrentMonth.ToString("MMMM");
        public string CurrentYearText  => CurrentMonth.ToString("yyyy");

        [ObservableProperty]
        private DateTime _selectedDate;

        [ObservableProperty]
        private bool _isNormalMode = true;

        [ObservableProperty]
        private bool _hasNoExercises;

        [ObservableProperty]
        private bool _hasExercises;

        public ObservableCollection<CalendarDayModel>   CalendarDays       { get; } = new();
        public ObservableCollection<PlannedWorkoutItem> ScheduledExercises { get; } = new();

        public PlannerViewModel(
            INavigationService navigationService,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IExerciseRepository exerciseRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _exerciseRepository          = exerciseRepository;
            _plannerStateService         = plannerStateService;

            SelectedDate = _plannerStateService.SelectedDate;
            CurrentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
        }

        public async Task InitializeAsync()
        {
            SelectedDate = _plannerStateService.SelectedDate;
            IsNormalMode = SelectedDate.Date >= DateTime.Today;

            // Rev 4: surface failures to the user instead of crashing silently.
            try
            {
                await _scheduledExerciseRepository.UpdateMissedExercisesAsync(UserId, DateTime.Today);
                await BuildCalendarAsync();      // populates _cachedRangeData
                await LoadScheduledExercisesAsync(); // consumes _cachedRangeData
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Load Error",
                    "Could not load your schedule. Please check your connection.", "OK");
                System.Diagnostics.Debug.WriteLine($"[ERROR] InitializeAsync: {ex.Message}");
            }
        }

        private async Task BuildCalendarAsync()
        {
            var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            int offset    = (int)firstDayOfMonth.DayOfWeek; // Sunday = 0
            var startDate = firstDayOfMonth.AddDays(-offset);
            var endDate   = startDate.AddDays(42);

            // Rev 2: single range query; result is cached for LoadScheduledExercisesAsync.
            _cachedRangeData = await _scheduledExerciseRepository
                .GetScheduledExercisesForRangeAsync(UserId, startDate, endDate);

            var tempDays = new List<CalendarDayModel>();

            for (int i = 0; i < 42; i++)
            {
                var day     = startDate.AddDays(i);
                var dayDate = day.Date;

                // Rev 5: centralized filter via IsVisibleForDate.
                var hasWorkouts = _cachedRangeData
                    .Where(se => se.ScheduledDate.Date == dayDate)
                    .Any(se => IsVisibleForDate(se, dayDate));

                tempDays.Add(new CalendarDayModel
                {
                    Date           = day,
                    IsSelected     = dayDate == SelectedDate.Date,
                    IsCurrentMonth = day.Month == CurrentMonth.Month,
                    HasExercises   = hasWorkouts
                });
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CalendarDays.Clear();
                foreach (var d in tempDays)
                    CalendarDays.Add(d);
                CurrentMonthYearText = CurrentMonth.ToString("MMMM yyyy");
            });
        }

        private async Task LoadScheduledExercisesAsync()
        {
            // Rev 2: use in-memory cache when available; avoids a redundant network round-trip.
            var rawScheduled = _cachedRangeData.Count > 0
                ? _cachedRangeData.Where(se => se.ScheduledDate.Date == SelectedDate.Date).ToList()
                : await _scheduledExerciseRepository.GetScheduledExercisesForDateAsync(UserId, SelectedDate);

            // Rev 5: single call to centralized filter.
            var scheduled = rawScheduled
                .Where(se => IsVisibleForDate(se, SelectedDate))
                .ToList();

            var catalog = await _exerciseRepository.GetAllAsync();

            var joined = (from s in scheduled
                          join c in catalog on s.ExerciseId equals c.ExerciseId
                          select new PlannedWorkoutItem
                          {
                              ScheduledExerciseId = s.Id,
                              ExerciseId          = c.ExerciseId,
                              Name                = c.Name,
                              GifUrl              = c.GifUrl,
                              BodyPart            = c.BodyPart,
                              Muscle              = c.Muscle,
                              Status              = s.Status
                          }).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScheduledExercises.Clear();
                foreach (var item in joined)
                    ScheduledExercises.Add(item);

                HasNoExercises = ScheduledExercises.Count == 0;
                HasExercises   = ScheduledExercises.Count > 0;
            });
        }

        // Rev 5: single source of truth for visibility logic.
        private static bool IsVisibleForDate(ScheduledExercise se, DateTime date)
            => date.Date < DateTime.Today
                ? se.Status == ExerciseStatus.Missed || se.Status == ExerciseStatus.Pending
                : se.Status == ExerciseStatus.Pending;

        [RelayCommand]
        private async Task SelectDay(CalendarDayModel day)
        {
            if (day == null) return;

            SelectedDate = day.Date;
            _plannerStateService.SelectedDate = SelectedDate;
            IsNormalMode = SelectedDate.Date >= DateTime.Today;

            if (SelectedDate.Date < DateTime.Today)
            {
                await Shell.Current.DisplayAlert("You selected a Previous Date",
                    "Exercises you have missed are listed here", "OK");
            }

            foreach (var calDay in CalendarDays)
                calDay.IsSelected = calDay.Date.Date == SelectedDate.Date;

            // Trigger visual refresh of collection on Main UI Thread
            var temp = CalendarDays.ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CalendarDays.Clear();
                foreach (var d in temp)
                    CalendarDays.Add(d);
            });

            await LoadScheduledExercisesAsync();
        }

        [RelayCommand]
        private async Task PreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
            await BuildCalendarAsync();
        }

        [RelayCommand]
        private async Task NextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
            await BuildCalendarAsync();
        }

        [RelayCommand]
        private async Task AddExercise()
        {
            await NavigationService.GoToAsync("WorkoutsPage");
        }

        [RelayCommand]
        private async Task StartWorkout(PlannedWorkoutItem item)
        {
            if (item == null) return;
            await NavigationService.GoToAsync($"ExercisePage?scheduledExerciseId={item.ScheduledExerciseId}");
        }

        [RelayCommand]
        private async Task DeleteExercise(PlannedWorkoutItem item)
        {
            if (item == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Scheduled Exercise",
                $"Are you sure you want to remove {item.Name} from your schedule?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    var success = await _scheduledExerciseRepository
                        .DeleteScheduledExerciseAsync(item.ScheduledExerciseId);

                    if (success)
                    {
                        // Rev 2: Build first to refresh cache, then Load uses the fresh cache.
                        await BuildCalendarAsync();
                        await LoadScheduledExercisesAsync();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error",
                            "Could not delete exercise. Please try again.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    // Rev 6: no raw stack trace shown to users.
                    await Shell.Current.DisplayAlert("Error",
                        $"Could not delete exercise: {ex.Message}", "OK");
                }
            }
        }
    }
}
