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

        [ObservableProperty]
        private string _currentMonthYearText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentMonthName))]
        [NotifyPropertyChangedFor(nameof(CurrentYearText))]
        private DateTime _currentMonth;

        public string CurrentMonthName => CurrentMonth.ToString("MMMM");
        public string CurrentYearText => CurrentMonth.ToString("yyyy");

        [ObservableProperty]
        private DateTime _selectedDate;

        [ObservableProperty]
        private bool _isNormalMode = true;

        [ObservableProperty]
        private bool _hasNoExercises;

        [ObservableProperty]
        private bool _hasExercises;

        public ObservableCollection<CalendarDayModel> CalendarDays { get; } = new();
        public ObservableCollection<PlannedWorkoutItem> ScheduledExercises { get; } = new();

        public PlannerViewModel(
            INavigationService navigationService,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IExerciseRepository exerciseRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _exerciseRepository = exerciseRepository;
            _plannerStateService = plannerStateService;

            SelectedDate = _plannerStateService.SelectedDate;
            CurrentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
        }

        public async Task InitializeAsync()
        {
            // Sync local selected date
            SelectedDate = _plannerStateService.SelectedDate;
            IsNormalMode = SelectedDate.Date >= DateTime.Today;

            // Mark missed exercises
            int userId = _plannerStateService.CurrentUser?.Id ?? 1;
            await _scheduledExerciseRepository.UpdateMissedExercisesAsync(userId, DateTime.Today);

            await BuildCalendarAsync();
            await LoadScheduledExercisesAsync();
        }

        private async Task BuildCalendarAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("[DEBUG] BuildCalendarAsync started");
            
            // Query for the visible range (6 weeks = 42 days)
            var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            int offset = (int)firstDayOfMonth.DayOfWeek; // Sunday = 0
            var startDate = firstDayOfMonth.AddDays(-offset);
            var endDate = startDate.AddDays(42);

            // Fetch scheduled exercises for the user in a single range query
            int userId = _plannerStateService.CurrentUser?.Id ?? 1;
            var scheduledForRange = await _scheduledExerciseRepository.GetScheduledExercisesForRangeAsync(userId, startDate, endDate);
            
            var tempDays = new List<CalendarDayModel>();

            // Build the list of days
            for (int i = 0; i < 42; i++)
            {
                var day = startDate.AddDays(i);
                var dayDate = day.Date;
                var hasWorkouts = false;

                // Check in the local list instead of making a database/network call per iteration
                var scheduledForDay = scheduledForRange.Where(se => se.ScheduledDate.Date == dayDate).ToList();
                if (dayDate < DateTime.Today)
                {
                    hasWorkouts = scheduledForDay.Any(se => se.Status == "MISSED" || se.Status == "PENDING");
                }
                else
                {
                    hasWorkouts = scheduledForDay.Any(se => se.Status == "PENDING");
                }

                tempDays.Add(new CalendarDayModel
                {
                    Date = day,
                    IsSelected = dayDate == SelectedDate.Date,
                    IsCurrentMonth = day.Month == CurrentMonth.Month,
                    HasExercises = hasWorkouts
                });
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CalendarDays.Clear();
                foreach (var d in tempDays)
                {
                    CalendarDays.Add(d);
                }
                CurrentMonthYearText = CurrentMonth.ToString("MMMM yyyy");
            });

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BuildCalendarAsync took {stopwatch.ElapsedMilliseconds} ms");
        }

        private async Task LoadScheduledExercisesAsync()
        {
            int userId = _plannerStateService.CurrentUser?.Id ?? 1;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadScheduledExercisesAsync: userId={userId}, SelectedDate={SelectedDate.ToShortDateString()}");

            var rawScheduled = await _scheduledExerciseRepository.GetScheduledExercisesForDateAsync(userId, SelectedDate);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadScheduledExercisesAsync: rawScheduled.Count={rawScheduled.Count}");
            foreach (var r in rawScheduled)
            {
                System.Diagnostics.Debug.WriteLine($"  -> Raw Item: Date={r.ScheduledDate.ToShortDateString()}, Status={r.Status}");
            }

            var scheduled = rawScheduled
                            .Where(se => SelectedDate.Date < DateTime.Today
                                ? (se.Status == "MISSED" || se.Status == "PENDING")
                                : se.Status == "PENDING")
                            .ToList();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadScheduledExercisesAsync: filtered scheduled.Count={scheduled.Count}");

            var catalog = await _exerciseRepository.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadScheduledExercisesAsync: catalog.Count={catalog.Count}");

            var joined = (from s in scheduled
                         join c in catalog on s.ExerciseId equals c.ExerciseId
                         select new PlannedWorkoutItem
                         {
                             ScheduledExerciseId = s.Id,
                             ExerciseId = c.ExerciseId,
                             Name = c.Name,
                             GifUrl = c.GifUrl,
                             BodyPart = c.BodyPart,
                             Muscle = c.Muscle,
                             Status = s.Status
                         }).ToList();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadScheduledExercisesAsync: joined.Count={joined.Count}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] MainThread: clearing and adding {joined.Count} items to ScheduledExercises");
                ScheduledExercises.Clear();
                foreach (var item in joined)
                {
                    ScheduledExercises.Add(item);
                }

                HasNoExercises = ScheduledExercises.Count == 0;
                HasExercises = ScheduledExercises.Count > 0;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] MainThread: HasExercises={HasExercises}, HasNoExercises={HasNoExercises}");
            });
        }

        [RelayCommand]
        private async Task SelectDay(CalendarDayModel day)
        {
            if (day == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] SelectDay called with NULL day");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectDay called for date: {day.Date.ToShortDateString()}, IsSelected={day.IsSelected}");
            SelectedDate = day.Date;
            _plannerStateService.SelectedDate = SelectedDate;
            IsNormalMode = SelectedDate.Date >= DateTime.Today;

            if (SelectedDate.Date < DateTime.Today)
            {
                await Shell.Current.DisplayAlert("You selected a Previous Date", "Exercises you have missed are listed here", "OK");
            }

            // Update selection in list
            foreach (var calDay in CalendarDays)
            {
                calDay.IsSelected = calDay.Date.Date == SelectedDate.Date;
            }

            // Trigger visual refresh of collection on Main UI Thread
            var temp = CalendarDays.ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CalendarDays.Clear();
                foreach (var d in temp)
                {
                    CalendarDays.Add(d);
                }
            });

            System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectedDate updated to: {SelectedDate.ToShortDateString()}. Reloading exercises...");
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
            // Navigate to category select
            await NavigationService.GoToAsync("WorkoutsPage");
        }

        [RelayCommand]
        private async Task StartWorkout(PlannedWorkoutItem item)
        {
            if (item == null) return;
            
            // Navigate to active workout page (ExercisePage)
            // Passes scheduledExerciseId as a query parameter
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
                await _scheduledExerciseRepository.DeleteScheduledExerciseAsync(item.ScheduledExerciseId);
                await LoadScheduledExercisesAsync();
                await BuildCalendarAsync(); // Refresh dots
            }
        }
    }
}
