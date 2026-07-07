using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    [QueryProperty(nameof(Category), "category")]
    [QueryProperty(nameof(Date), "date")]
    public partial class MealCategoryViewModel : BaseViewModel
    {
        private readonly IMealLogRepository _mealLogRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _date = string.Empty;

        [ObservableProperty]
        private string _categoryTitle = "Meal Details";

        [ObservableProperty]
        private double _categoryCaloriesTotal;

        [ObservableProperty]
        private string _newFoodName = string.Empty;

        [ObservableProperty]
        private string _newCaloriesText = string.Empty;

        [ObservableProperty]
        private string _validationError = string.Empty;

        public ObservableCollection<MealLog> MealLogs { get; } = new();

        private User? _currentUser;

        public MealCategoryViewModel(
            INavigationService navigationService,
            IMealLogRepository mealLogRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _mealLogRepository = mealLogRepository;
            _plannerStateService = plannerStateService;
        }

        partial void OnCategoryChanged(string value)
        {
            UpdateTitle();
            _ = LoadLogsAsync();
        }

        partial void OnDateChanged(string value)
        {
            _ = LoadLogsAsync();
        }

        private void UpdateTitle()
        {
            CategoryTitle = $"{Category} Log";
        }

        [RelayCommand]
        public async Task LoadLogsAsync()
        {
            if (string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(Date))
                return;

            IsBusy = true;
            try
            {
                _currentUser = _plannerStateService.CurrentUser;
                if (_currentUser == null) return;

                var logs = await _mealLogRepository.GetMealLogsForDateAsync(_currentUser.Id, Date);
                var categoryLogs = logs.Where(x => x.Category.Equals(Category, StringComparison.OrdinalIgnoreCase)).ToList();

                MealLogs.Clear();
                foreach (var log in categoryLogs)
                {
                    MealLogs.Add(log);
                }

                CategoryCaloriesTotal = categoryLogs.Sum(x => x.Calories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load meal logs: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddLogAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AddLogAsync called. Name: '{NewFoodName}', Calories: '{NewCaloriesText}', Category: '{Category}', Date: '{Date}'");
            if (_currentUser == null)
            {
                _currentUser = _plannerStateService.CurrentUser;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Fetched current user from state. Null? {_currentUser == null}");
                if (_currentUser == null) return;
            }

            ValidationError = string.Empty;

            if (string.IsNullOrWhiteSpace(NewFoodName))
            {
                ValidationError = "Food name is required.";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Validation failed: food name empty");
                return;
            }

            if (!double.TryParse(NewCaloriesText, out double calories) || calories <= 0)
            {
                ValidationError = "Calories must be a positive number.";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Validation failed: invalid calories '{NewCaloriesText}'");
                return;
            }

            var log = new MealLog
            {
                UserId = _currentUser.Id,
                Category = Category,
                FoodName = NewFoodName.Trim(),
                Calories = calories,
                LogDate = Date
            };

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Saving log to repository...");
            var success = await _mealLogRepository.AddMealLogAsync(log);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Save success: {success}");
            if (success)
            {
                NewFoodName = string.Empty;
                NewCaloriesText = string.Empty;
                await LoadLogsAsync();
            }
            else
            {
                ValidationError = "Failed to save log entry.";
            }
        }

        [RelayCommand]
        private async Task DeleteLogAsync(MealLog log)
        {
            if (log == null) return;

            var success = await _mealLogRepository.DeleteMealLogAsync(log.Id);
            if (success)
            {
                await LoadLogsAsync();
            }
        }
    }
}
