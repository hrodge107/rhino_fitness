using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class ProfileEditViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;

        // High precision underlying state
        private double _rawHeightInches;
        private double _rawWeightKg;

        [ObservableProperty]
        private string _heightUnit = "ft/in"; // "ft/in" or "cm"

        [ObservableProperty]
        private string _weightUnit = "kg"; // "kg" or "lbs"

        // Inputs for ft/in
        [ObservableProperty]
        private string _heightFeetText = "5";

        [ObservableProperty]
        private string _heightInchesText = "9";

        // Inputs for cm
        [ObservableProperty]
        private string _heightCmText = "175.3";

        // Weight Input (used for both kg and lbs)
        [ObservableProperty]
        private string _weightValueText = "75.0";

        // Goal & Activity Level
        [ObservableProperty]
        private string _activityLevel = "Moderately Active";

        [ObservableProperty]
        private string _goal = "Maintain";

        // Subtitle previews
        [ObservableProperty]
        private string _heightMirrorText = string.Empty;

        [ObservableProperty]
        private string _weightMirrorText = string.Empty;

        [ObservableProperty]
        private string _validationError = string.Empty;

        public bool IsHeightFtIn => HeightUnit == "ft/in";
        public bool IsHeightCm => HeightUnit == "cm";

        public ProfileEditViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            var user = _plannerStateService.CurrentUser;
            if (user != null)
            {
                HeightUnit = user.HeightUnit;
                WeightUnit = user.WeightUnit;
                ActivityLevel = string.IsNullOrEmpty(user.ActivityLevel) ? "Moderately Active" : user.ActivityLevel;
                Goal = string.IsNullOrEmpty(user.Goal) ? "Maintain" : user.Goal;

                // Load height state
                if (user.HeightUnit == "cm")
                {
                    _rawHeightInches = user.HeightValue / 2.54;
                    HeightCmText = user.HeightValue.ToString("F1");
                }
                else
                {
                    _rawHeightInches = user.HeightValue;
                    int feet = (int)(user.HeightValue / 12);
                    double inches = user.HeightValue % 12;
                    HeightFeetText = feet.ToString();
                    HeightInchesText = inches.ToString("F1");
                }

                // Load weight state
                if (user.WeightUnit == "kg")
                {
                    _rawWeightKg = user.WeightValue;
                    WeightValueText = user.WeightValue.ToString("F1");
                }
                else
                {
                    _rawWeightKg = user.WeightValue / 2.20462262185;
                    WeightValueText = user.WeightValue.ToString("F1");
                }

                UpdatePreviews();
            }
        }

        [RelayCommand]
        private void SelectActivityLevel(string selectedLevel)
        {
            ActivityLevel = selectedLevel;
        }

        [RelayCommand]
        private void SelectGoal(string selectedGoal)
        {
            Goal = selectedGoal;
        }

        [RelayCommand]
        private void ToggleHeightUnit()
        {
            // Sync inputs to raw first
            UpdateRawHeightFromInputs();

            if (HeightUnit == "ft/in")
            {
                HeightUnit = "cm";
                // Show rounded presentation value of cm
                double cm = _rawHeightInches * 2.54;
                HeightCmText = cm.ToString("F1");
            }
            else
            {
                HeightUnit = "ft/in";
                // Show rounded presentation value of ft/in
                double inches = _rawHeightInches;
                int feet = (int)(inches / 12);
                double remInches = inches % 12;
                if (Math.Round(remInches, 1) >= 12.0)
                {
                    feet++;
                    remInches = 0;
                }
                HeightFeetText = feet.ToString();
                HeightInchesText = remInches.ToString("F1");
            }

            OnPropertyChanged(nameof(IsHeightFtIn));
            OnPropertyChanged(nameof(IsHeightCm));
            UpdatePreviews();
        }

        [RelayCommand]
        private void ToggleWeightUnit()
        {
            // Sync inputs to raw first
            UpdateRawWeightFromInputs();

            if (WeightUnit == "kg")
            {
                WeightUnit = "lbs";
                double lbs = _rawWeightKg * 2.20462262185;
                WeightValueText = lbs.ToString("F1");
            }
            else
            {
                WeightUnit = "kg";
                WeightValueText = _rawWeightKg.ToString("F1");
            }

            UpdatePreviews();
        }

        // Methods to parse current fields and update previews real-time
        public void OnHeightInputChanged()
        {
            UpdateRawHeightFromInputs();
            UpdatePreviews();
        }

        public void OnWeightInputChanged()
        {
            UpdateRawWeightFromInputs();
            UpdatePreviews();
        }

        private void UpdateRawHeightFromInputs()
        {
            if (HeightUnit == "cm")
            {
                if (double.TryParse(HeightCmText, out double cmVal) && cmVal > 0)
                {
                    _rawHeightInches = cmVal / 2.54;
                }
            }
            else
            {
                double.TryParse(HeightFeetText, out double ftVal);
                double.TryParse(HeightInchesText, out double inVal);
                if (ftVal >= 0 && inVal >= 0)
                {
                    _rawHeightInches = ftVal * 12 + inVal;
                }
            }
        }

        private void UpdateRawWeightFromInputs()
        {
            if (double.TryParse(WeightValueText, out double wVal) && wVal > 0)
            {
                if (WeightUnit == "kg")
                {
                    _rawWeightKg = wVal;
                }
                else
                {
                    _rawWeightKg = wVal / 2.20462262185;
                }
            }
        }

        private void UpdatePreviews()
        {
            // Update Height Preview
            if (HeightUnit == "cm")
            {
                double inches = _rawHeightInches;
                int feet = (int)(inches / 12);
                double remInches = Math.Round(inches % 12, 1);
                if (remInches >= 12.0)
                {
                    feet++;
                    remInches = 0;
                }
                HeightMirrorText = $"≈ {feet}'{remInches:F1}\"";
            }
            else
            {
                double cm = _rawHeightInches * 2.54;
                HeightMirrorText = $"≈ {cm:F1} cm";
            }

            // Update Weight Preview
            if (WeightUnit == "kg")
            {
                double lbs = _rawWeightKg * 2.20462262185;
                WeightMirrorText = $"≈ {lbs:F1} lbs";
            }
            else
            {
                WeightMirrorText = $"≈ {_rawWeightKg:F1} kg";
            }
        }

        [RelayCommand]
        private async Task SaveMetrics()
        {
            ValidationError = string.Empty;

            var isOffline = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet;
            if (isOffline)
            {
                ValidationError = "Network disconnected. Could not save changes.";
                return;
            }

            // Final sync
            UpdateRawHeightFromInputs();
            UpdateRawWeightFromInputs();

            // Validate
            if (HeightUnit == "cm")
            {
                if (!double.TryParse(HeightCmText, out double cmVal) || cmVal <= 0 || cmVal > 300)
                {
                    ValidationError = "Please enter a valid height in cm (0 to 300).";
                    return;
                }
            }
            else
            {
                if (!double.TryParse(HeightFeetText, out double ftVal) || ftVal < 1 || ftVal > 9 ||
                    !double.TryParse(HeightInchesText, out double inVal) || inVal < 0 || inVal >= 12)
                {
                    ValidationError = "Please enter valid feet (1 to 9) and inches (0 to 11.9).";
                    return;
                }
            }

            if (!double.TryParse(WeightValueText, out double weightVal) || weightVal <= 0 || weightVal > 600)
            {
                ValidationError = $"Please enter a valid weight (0 to 600 {WeightUnit}).";
                return;
            }

            var user = _plannerStateService.CurrentUser;
            if (user != null)
            {
                IsBusy = true;
                try
                {
                    user.HeightUnit = HeightUnit;
                    user.WeightUnit = WeightUnit;
                    user.ActivityLevel = ActivityLevel;
                    user.Goal = Goal;

                    // Save standard value representations
                    if (HeightUnit == "cm")
                    {
                        // For CM, HeightValue stores CM
                        user.HeightValue = double.Parse(HeightCmText);
                    }
                    else
                    {
                        // For ft/in, HeightValue stores raw total inches
                        user.HeightValue = _rawHeightInches;
                    }

                    user.WeightValue = double.Parse(WeightValueText);

                    // Recalculate targets based on updated metrics, activity, and goal
                    double heightCm = _rawHeightInches * 2.54;
                    double weightKg = _rawWeightKg;
                    int age = user.Age > 0 ? user.Age : 25;
                    double bmr = NutritionCalculator.CalculateBmr(weightKg, heightCm, age, user.Gender);
                    double tdee = NutritionCalculator.CalculateTdee(bmr, ActivityLevel);
                    user.CalorieLimit = NutritionCalculator.CalculateCalories(tdee, Goal);
                    user.WaterLimit = NutritionCalculator.CalculateWater(weightKg);

                    var success = await _userRepository.UpdateUserAsync(user);
                    if (success)
                    {
                        await NavigationService.GoBackAsync();
                    }
                    else
                    {
                        ValidationError = "Network disconnected. Could not save changes.";
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await NavigationService.GoBackAsync();
        }
    }
}
