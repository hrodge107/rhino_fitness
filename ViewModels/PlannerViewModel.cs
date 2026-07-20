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
        public bool IsPending => Status == "PENDING";
        public bool IsCompleted => Status == "COMPLETED";
        public bool IsNotMissed => !IsMissed && !IsCompleted;
        public bool IsActionable => IsPending;
        public string CardBgColor => IsMissed ? "#3A2020" : "#2A2A2E";
    }

    public partial class PlannerViewModel : BaseViewModel
    {
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IPlannerStateService _plannerStateService;

        // Cache 42-day range populated by BuildCalendarAsync
        private List<ScheduledExercise> _cachedRangeData = new();
        private List<PlannedWorkoutItem> _allCurrentExercises = new();

        // Status constants
        private static class ExerciseStatus
        {
            public const string Pending = "PENDING";
            public const string Missed  = "MISSED";
            public const string Completed = "COMPLETED";
        }

        // Throw if not authenticated
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

        [ObservableProperty]
        private ObservableCollection<string> _filterOptions = new();

        [ObservableProperty]
        private string _selectedFilter = "All";

        partial void OnSelectedFilterChanged(string value)
        {
            if (value == null) return;
            ApplyFilterAndSort();
        }

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

            // Load exercises, report errors to UI
            try
            {
                await _scheduledExerciseRepository.UpdateMissedExercisesAsync(UserId, DateTime.Today);
                UpdateFilterOptions(SelectedDate);
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
            int offset    = (int)firstDayOfMonth.DayOfWeek;
            var startDate = firstDayOfMonth.AddDays(-offset);
            var endDate   = startDate.AddDays(42);

            // Single range query cached for load
            _cachedRangeData = await _scheduledExerciseRepository
                .GetScheduledExercisesForRangeAsync(UserId, startDate, endDate);

            var tempDays = new List<CalendarDayModel>();

            for (int i = 0; i < 42; i++)
            {
                var day     = startDate.AddDays(i);
                var dayDate = day.Date;

                // Filter via IsVisibleForDate
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
            // Use in-memory cache if possible
            var rawScheduled = _cachedRangeData.Count > 0
                ? _cachedRangeData.Where(se => se.ScheduledDate.Date == SelectedDate.Date).ToList()
                : await _scheduledExerciseRepository.GetScheduledExercisesForDateAsync(UserId, SelectedDate);

            // Filter exercises
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

            _allCurrentExercises = joined;
            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            var filtered = _allCurrentExercises.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedFilter) && !string.Equals(SelectedFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(x => string.Equals(x.Status, SelectedFilter, StringComparison.OrdinalIgnoreCase));
            }

            var sorted = filtered
                .OrderBy(x => x.IsCompleted ? 1 : 0)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScheduledExercises.Clear();
                foreach (var item in sorted)
                    ScheduledExercises.Add(item);

                HasNoExercises = ScheduledExercises.Count == 0;
                HasExercises   = ScheduledExercises.Count > 0;
            });
        }

        private void UpdateFilterOptions(DateTime date)
        {
            var options = new List<string> { "All" };
            if (date.Date < DateTime.Today)
            {
                options.Add("Completed");
                options.Add("Missed");
            }
            else
            {
                options.Add("Pending");
                options.Add("Completed");
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilterOptions.Clear();
                foreach (var opt in options)
                    FilterOptions.Add(opt);

#pragma warning disable MVVMTK0034
                _selectedFilter = "All";
#pragma warning restore MVVMTK0034
                OnPropertyChanged(nameof(SelectedFilter));
            });
        }

        // Centralized visibility logic
        private static bool IsVisibleForDate(ScheduledExercise se, DateTime date)
            => date.Date < DateTime.Today
                ? se.Status == ExerciseStatus.Missed || se.Status == ExerciseStatus.Pending || se.Status == ExerciseStatus.Completed
                : se.Status == ExerciseStatus.Pending || se.Status == ExerciseStatus.Completed;

        [RelayCommand]
        private async Task SelectDay(CalendarDayModel day)
        {
            if (day == null) return;

            SelectedDate = day.Date;
            _plannerStateService.SelectedDate = SelectedDate;
            IsNormalMode = SelectedDate.Date >= DateTime.Today;

            foreach (var calDay in CalendarDays)
                calDay.IsSelected = calDay.Date.Date == SelectedDate.Date;

            // Refresh collection on UI thread
            var temp = CalendarDays.ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CalendarDays.Clear();
                foreach (var d in temp)
                    CalendarDays.Add(d);
            });

            UpdateFilterOptions(SelectedDate);
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
                        // Rebuild calendar cache and reload
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
                    // Friendly error message without stack trace
                    await Shell.Current.DisplayAlert("Error",
                        $"Could not delete exercise: {ex.Message}", "OK");
                }
            }
        }
    }
}
