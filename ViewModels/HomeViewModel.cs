using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Initials))]
        private string _userName = "Person";

        [ObservableProperty]
        private string _currentDateText = string.Empty;

        public ObservableCollection<DayModel> WeekDays { get; } = new();

        private readonly IPlannerStateService _plannerStateService;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;

        [ObservableProperty]
        private int _totalExercisesCount;

        [ObservableProperty]
        private int _completedExercisesCount;

        [ObservableProperty]
        private double _progressPercentage;

        [ObservableProperty]
        private string _progressText = "0 of 0 completed";

        [ObservableProperty]
        private string _motivationalText = "No exercises scheduled today.";

        [ObservableProperty]
        private string _targetCaloriesText = "2,200 kcal";

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UserName))
                    return "?";
                var parts = UserName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return "?";
                if (parts.Length == 1)
                    return parts[0][0].ToString().ToUpper();
                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }

        public HomeViewModel(
            INavigationService navigationService,
            IPlannerStateService plannerStateService,
            IScheduledExerciseRepository scheduledExerciseRepository) : base(navigationService)
        {
            _plannerStateService = plannerStateService;
            _scheduledExerciseRepository = scheduledExerciseRepository;
            var today = DateTime.Now;
            CurrentDateText = today.ToString("MMMM d, dddd");
            BuildWeek(today);
            UpdateUserName();
        }

        public void UpdateUserName()
        {
            UserName = _plannerStateService.CurrentUser?.Name ?? "Adam";
        }

        public async Task LoadTodayProgressAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null) return;

            var today = DateTime.Today;
            var exercises = await _scheduledExerciseRepository.GetScheduledExercisesForDateAsync(user.Id, today);

            TotalExercisesCount = exercises.Count;
            CompletedExercisesCount = exercises.Count(e => e.Status == "COMPLETED");

            if (TotalExercisesCount > 0)
            {
                ProgressPercentage = (double)CompletedExercisesCount / TotalExercisesCount;
                ProgressText = $"{CompletedExercisesCount} of {TotalExercisesCount} completed";

                if (CompletedExercisesCount == TotalExercisesCount)
                {
                    MotivationalText = "Awesome! All workouts finished today. 🎉";
                }
                else if (CompletedExercisesCount > 0)
                {
                    MotivationalText = "Great start! Keep pushing. 💪";
                }
                else
                {
                    MotivationalText = "Time to crush your workouts! 🔥";
                }
            }
            else
            {
                ProgressPercentage = 0.0;
                ProgressText = "0 of 0 completed";
                MotivationalText = "No exercises scheduled for today.";
            }

            // Set Target Calories based on weight
            double weight = user.WeightValue > 0 ? user.WeightValue : 70;
            double calories = user.WeightUnit == "lbs" ? (weight / 2.2046) * 30 : weight * 30;
            TargetCaloriesText = $"{calories:F0} kcal";
        }

        private void BuildWeek(DateTime today)
        {
            // Find the Sunday of the current week.
            int offset = (int)today.DayOfWeek; // Sunday = 0
            var sunday = today.AddDays(-offset);

            string[] names = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                var day = sunday.AddDays(i);
                WeekDays.Add(new DayModel
                {
                    DayName = names[i],
                    DayNumber = day.Day,
                    IsToday = day.Date == today.Date
                });
            }
        }

        [RelayCommand]
        private Task NavigateToPlanner() => NavigationService.GoToAsync("PlannerPage");

        [RelayCommand]
        private Task NavigateToWorkouts() => NavigationService.GoToAsync("WorkoutsPage");

        [RelayCommand]
        private Task NavigateToNutrition() => NavigationService.GoToAsync("NutritionPage");

        [RelayCommand]
        private Task NavigateToProfile() => NavigationService.GoToAsync("ProfilePage");
    }
}
