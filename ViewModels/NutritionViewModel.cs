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
        private readonly IWaterLogRepository _waterLogRepository;
        private List<WaterLog> _waterLogs = new();

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

        [ObservableProperty]
        private bool _isOzActive;

        [ObservableProperty]
        private string _customAmountText = string.Empty;

        [ObservableProperty]
        private string _actionLogText = string.Empty;

        [ObservableProperty]
        private string _validationError = string.Empty;

        [ObservableProperty]
        private bool _isCustomInputVisible;

        [ObservableProperty]
        private bool _isEditingHydrationTarget;

        [ObservableProperty]
        private string _newHydrationTargetText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HydrationPercentage))]
        [NotifyPropertyChangedFor(nameof(HydrationFillHeight))]
        [NotifyPropertyChangedFor(nameof(DailyHydrationDisplay))]
        [NotifyPropertyChangedFor(nameof(IsPastTarget))]
        [NotifyPropertyChangedFor(nameof(ProgressBarColor))]
        [NotifyPropertyChangedFor(nameof(OverfillNotificationText))]
        private double _dailyHydrationAmount; // stored in mL

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HydrationPercentage))]
        [NotifyPropertyChangedFor(nameof(HydrationFillHeight))]
        [NotifyPropertyChangedFor(nameof(DailyHydrationDisplay))]
        [NotifyPropertyChangedFor(nameof(IsPastTarget))]
        [NotifyPropertyChangedFor(nameof(ProgressBarColor))]
        [NotifyPropertyChangedFor(nameof(OverfillNotificationText))]
        private double _dailyHydrationTarget = 3000; // stored in mL

        public double HydrationPercentage => DailyHydrationTarget > 0 ? Math.Min(DailyHydrationAmount / DailyHydrationTarget, 1.0) : 0;

        public double HydrationFillHeight => HydrationPercentage * 180;

        public bool IsPastTarget => DailyHydrationAmount > DailyHydrationTarget;

        public string ProgressBarColor => IsPastTarget || HydrationPercentage >= 1.0 ? "#00DF89" : "#4DA3FF";

        public string OverfillNotificationText
        {
            get
            {
                if (IsPastTarget)
                {
                    double excess = DailyHydrationAmount - DailyHydrationTarget;
                    if (IsOzActive)
                    {
                        double excessOz = excess / 29.5735;
                        return $"Awesome! You are past your daily target (+{excessOz:N1} oz)";
                    }
                    else
                    {
                        return $"Awesome! You are past your daily target (+{(int)excess} mL)";
                    }
                }
                return string.Empty;
            }
        }

        public string DailyHydrationDisplay
        {
            get
            {
                if (IsOzActive)
                {
                    double amountOz = DailyHydrationAmount / 29.5735;
                    double targetOz = DailyHydrationTarget / 29.5735;
                    return $"{amountOz:N1} oz / {targetOz:N1} oz";
                }
                else
                {
                    return $"{(int)DailyHydrationAmount} mL / {(int)DailyHydrationTarget} mL";
                }
            }
        }

        public string Preset1Text => IsOzActive ? "8.5 oz" : "250 mL";
        public string Preset2Text => IsOzActive ? "11.8 oz" : "350 mL";
        public string Preset3Text => IsOzActive ? "16.9 oz" : "500 mL";
        public string ManualAmountPlaceholder => IsOzActive ? "Amount (oz)" : "Amount (mL)";

        private User? _currentUser;
        private readonly string _todayStr;

        public NutritionViewModel(
            INavigationService navigationService,
            IMealLogRepository mealLogRepository,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService,
            IWaterLogRepository waterLogRepository) : base(navigationService)
        {
            _mealLogRepository = mealLogRepository;
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
            _waterLogRepository = waterLogRepository;

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

                // Load Water logs
                _waterLogs = await _waterLogRepository.GetWaterLogsForDateAsync(_currentUser.Id, _todayStr);
                DailyHydrationAmount = _waterLogs.Sum(x => x.Amount);

                // ponytail: load persisted target
                DailyHydrationTarget = Preferences.Default.Get($"WaterTarget_{_currentUser.Id}", 3000.0);
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

        [RelayCommand]
        private async Task LogPresetAmountAsync(double amountMl)
        {
            if (_currentUser == null) return;

            var log = new WaterLog
            {
                UserId = _currentUser.Id,
                Amount = amountMl,
                LogDate = _todayStr
            };

            if (await _waterLogRepository.AddWaterLogAsync(log))
            {
                _waterLogs.Add(log);
                DailyHydrationAmount = _waterLogs.Sum(x => x.Amount);
                if (IsOzActive)
                {
                    ActionLogText = $"Added {amountMl / 29.5735:N1} oz";
                }
                else
                {
                    ActionLogText = $"Added {(int)amountMl} mL";
                }
            }
        }

        [RelayCommand]
        private async Task LogCustomAmountAsync()
        {
            if (_currentUser == null) return;
            ValidationError = string.Empty;

            if (!double.TryParse(CustomAmountText, out double inputVal))
            {
                ValidationError = "Enter a valid number.";
                return;
            }

            // Convert to mL if inputs are in oz for the range validation (1 to 9,999 mL)
            double amountMl = IsOzActive ? inputVal * 29.5735 : inputVal;

            if (amountMl < 1 || amountMl > 9999)
            {
                if (IsOzActive)
                {
                    ValidationError = $"Range: {(1 / 29.5735):N1} to {(9999 / 29.5735):N1} oz";
                }
                else
                {
                    ValidationError = "Range: 1 to 9,999 mL";
                }
                return;
            }

            var log = new WaterLog
            {
                UserId = _currentUser.Id,
                Amount = amountMl,
                LogDate = _todayStr
            };

            if (await _waterLogRepository.AddWaterLogAsync(log))
            {
                _waterLogs.Add(log);
                DailyHydrationAmount = _waterLogs.Sum(x => x.Amount);
                CustomAmountText = string.Empty;
                IsCustomInputVisible = false; // Collapse entry on success
                if (IsOzActive)
                {
                    ActionLogText = $"Added {inputVal:N1} oz";
                }
                else
                {
                    ActionLogText = $"Added {(int)inputVal} mL";
                }
            }
            else
            {
                ValidationError = "Failed to save entry.";
            }
        }

        [RelayCommand]
        private async Task UndoLastLogAsync()
        {
            if (_currentUser == null || !_waterLogs.Any()) return;

            var lastLog = _waterLogs.Last();
            if (await _waterLogRepository.DeleteWaterLogAsync(lastLog.Id))
            {
                _waterLogs.Remove(lastLog);
                DailyHydrationAmount = _waterLogs.Sum(x => x.Amount);
                if (IsOzActive)
                {
                    ActionLogText = $"Undid log of {lastLog.Amount / 29.5735:N1} oz";
                }
                else
                {
                    ActionLogText = $"Undid log of {(int)lastLog.Amount} mL";
                }
            }
        }

        [RelayCommand]
        private void ToggleUnit()
        {
            IsOzActive = !IsOzActive;
            OnPropertyChanged(nameof(Preset1Text));
            OnPropertyChanged(nameof(Preset2Text));
            OnPropertyChanged(nameof(Preset3Text));
            OnPropertyChanged(nameof(ManualAmountPlaceholder));
            OnPropertyChanged(nameof(DailyHydrationDisplay));
            OnPropertyChanged(nameof(OverfillNotificationText));
        }

        [RelayCommand]
        private void ToggleCustomInput()
        {
            IsCustomInputVisible = !IsCustomInputVisible;
            if (!IsCustomInputVisible)
            {
                ValidationError = string.Empty;
            }
        }

        [RelayCommand]
        private void EditHydrationTarget()
        {
            if (IsOzActive)
            {
                NewHydrationTargetText = $"{DailyHydrationTarget / 29.5735:N1}";
            }
            else
            {
                NewHydrationTargetText = $"{(int)DailyHydrationTarget}";
            }
            IsEditingHydrationTarget = true;
        }

        [RelayCommand]
        private void SaveHydrationTarget()
        {
            if (!double.TryParse(NewHydrationTargetText, out double parsedVal) || parsedVal <= 0)
            {
                ValidationError = "Enter a positive number.";
                return;
            }

            // Convert to mL if inputs are in oz
            double targetMl = IsOzActive ? parsedVal * 29.5735 : parsedVal;

            if (targetMl < 1 || targetMl > 99999)
            {
                ValidationError = "Target must be 1 to 99,999 mL.";
                return;
            }

            DailyHydrationTarget = targetMl;
            IsEditingHydrationTarget = false;
            ValidationError = string.Empty;

            if (_currentUser != null)
            {
                Preferences.Default.Set($"WaterTarget_{_currentUser.Id}", targetMl);
            }
        }

        [RelayCommand]
        private void CancelEditHydrationTarget()
        {
            IsEditingHydrationTarget = false;
            ValidationError = string.Empty;
        }
    }
}
