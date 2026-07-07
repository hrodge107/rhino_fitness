using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;
using FitnessApp.Views;

namespace FitnessApp.ViewModels
{
    public partial class NutritionViewModel : BaseViewModel
    {
        private readonly IMealLogRepository _mealLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;

        public CalorieProgressDrawable ProgressDrawable { get; } = new();

        [ObservableProperty]
        private double _currentCalories;

        [ObservableProperty]
        private double _calorieLimit = 2000;

        [ObservableProperty]
        private double _breakfastCalories;

        [ObservableProperty]
        private double _lunchCalories;

        [ObservableProperty]
        private double _dinnerCalories;

        [ObservableProperty]
        private double _snackCalories;

        [ObservableProperty]
        private string _currentDateText = string.Empty;

        [ObservableProperty]
        private bool _isEditingLimit;

        [ObservableProperty]
        private string _newCalorieLimitText = string.Empty;

        // ponytail: placeholder hydration properties, logic-ready
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HydrationPercentage))]
        [NotifyPropertyChangedFor(nameof(HydrationFillHeight))]
        private double _dailyHydrationAmount = 2.22;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HydrationPercentage))]
        [NotifyPropertyChangedFor(nameof(HydrationFillHeight))]
        private double _dailyHydrationTarget = 3.00;

        public double HydrationPercentage => DailyHydrationTarget > 0 ? Math.Min(DailyHydrationAmount / DailyHydrationTarget, 1.0) : 0;

        public double HydrationFillHeight => HydrationPercentage * 180;

        private User? _currentUser;
        private readonly string _todayStr;

        public NutritionViewModel(
            INavigationService navigationService,
            IMealLogRepository mealLogRepository,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _mealLogRepository = mealLogRepository;
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;

            var today = DateTime.Today;
            _todayStr = today.ToString("yyyy-MM-dd");
            CurrentDateText = "Today, " + today.ToString("ddd MMM dd").ToUpper();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                _currentUser = _plannerStateService.CurrentUser;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NutritionViewModel loaded user. Null? {_currentUser == null}");
                if (_currentUser == null) return;

                // Sync profile calorie limit if possible
                var updatedUser = await _userRepository.GetByIdAsync(_currentUser.Id);
                if (updatedUser != null)
                {
                    _currentUser = updatedUser;
                }

                CalorieLimit = _currentUser.CalorieLimit;
                ProgressDrawable.CalorieLimit = CalorieLimit;

                // Get category calorie sums
                BreakfastCalories = await _mealLogRepository.GetCategoryCaloriesAsync(_currentUser.Id, _todayStr, "Breakfast");
                LunchCalories = await _mealLogRepository.GetCategoryCaloriesAsync(_currentUser.Id, _todayStr, "Lunch");
                DinnerCalories = await _mealLogRepository.GetCategoryCaloriesAsync(_currentUser.Id, _todayStr, "Dinner");
                SnackCalories = await _mealLogRepository.GetCategoryCaloriesAsync(_currentUser.Id, _todayStr, "Snack");

                // Get overall current calories
                CurrentCalories = await _mealLogRepository.GetDailyCaloriesAsync(_currentUser.Id, _todayStr);
                ProgressDrawable.CurrentCalories = CurrentCalories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load nutrition data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void EditLimit()
        {
            NewCalorieLimitText = CalorieLimit.ToString();
            IsEditingLimit = true;
        }

        [RelayCommand]
        private async Task SaveLimitAsync()
        {
            if (_currentUser == null) return;

            if (double.TryParse(NewCalorieLimitText, out double newLimit) && newLimit > 0)
            {
                IsEditingLimit = false;
                CalorieLimit = newLimit;
                ProgressDrawable.CalorieLimit = CalorieLimit;

                _currentUser.CalorieLimit = newLimit;
                await _userRepository.UpdateUserAsync(_currentUser);

                // Reload data to redraw
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private void CancelEditLimit()
        {
            IsEditingLimit = false;
        }

        [RelayCommand]
        private async Task NavigateToCategoryAsync(string category)
        {
            // Navigate to MealCategoryPage passing category and date
            await NavigationService.GoToAsync($"{nameof(MealCategoryPage)}?category={category}&date={_todayStr}");
        }
    }
}
