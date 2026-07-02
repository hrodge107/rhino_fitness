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
        private string _userName = "Person";

        [ObservableProperty]
        private string _currentDateText = string.Empty;

        public ObservableCollection<DayModel> WeekDays { get; } = new();

        public HomeViewModel(INavigationService navigationService) : base(navigationService)
        {
            var today = DateTime.Now;
            CurrentDateText = today.ToString("MMMM d, dddd");
            BuildWeek(today);
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
    }
}
