using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;
using Microsoft.Maui.Networking;

namespace FitnessApp.ViewModels
{
    public partial class OnboardingViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;

        // Step number: 1 to 8
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ContinueButtonText))]
        private int _currentStep = 1;

        // Step 2: Gender
        [ObservableProperty]
        private string _gender = string.Empty; // "Male", "Female", "Others"

        // Step 3: Age
        [ObservableProperty]
        private string _ageText = string.Empty;

        // Step 4: Activity Level
        [ObservableProperty]
        private string _activityLevel = "Moderately Active"; // "Sedentary", "Lightly Active", "Moderately Active", "Very Active"

        // Step 5: Height & Weight
        private double _rawHeightInches;
        private double _rawWeightKg;

        [ObservableProperty]
        private string _heightUnit = "ft/in"; // "ft/in" or "cm"

        [ObservableProperty]
        private string _weightUnit = "kg"; // "kg" or "lbs"

        [ObservableProperty]
        private string _heightFeetText = "5";

        [ObservableProperty]
        private string _heightInchesText = "9";

        [ObservableProperty]
        private string _heightCmText = "175.3";

        [ObservableProperty]
        private string _weightValueText = "70.0";

        [ObservableProperty]
        private string _heightMirrorText = string.Empty;

        [ObservableProperty]
        private string _weightMirrorText = string.Empty;

        // Step 5: BMI Display
        [ObservableProperty]
        private string _bmiText = "--";

        [ObservableProperty]
        private string _bmiStatus = string.Empty;

        [ObservableProperty]
        private string _bmiBgColor = "#2A2A2E";

        // Step 6: Goal
        [ObservableProperty]
        private string _goal = "Maintain"; // "Lose Weight", "Maintain", "Gain Weight", "Just Track"

        // Step 7: Targets
        [ObservableProperty]
        private string _calorieLimitText = "2000";

        [ObservableProperty]
        private string _waterLimitText = "3000";

        [ObservableProperty]
        private string _validationError = string.Empty;

        public string ContinueButtonText => CurrentStep == 8 ? "Let's go" : "Continue";

        public bool IsHeightFtIn => HeightUnit == "ft/in";
        public bool IsHeightCm => HeightUnit == "cm";

        public OnboardingViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
            
            // set defaults for raw
            _rawHeightInches = 69; // 5'9"
            _rawWeightKg = 70.0;
            
            UpdatePreviews();
            CalculateBmi();
        }

        [RelayCommand]
        private void SelectGender(string selectedGender)
        {
            Gender = selectedGender;
            ValidationError = string.Empty;
        }

        [RelayCommand]
        private void SelectActivityLevel(string selectedLevel)
        {
            ActivityLevel = selectedLevel;
            ValidationError = string.Empty;
        }

        [RelayCommand]
        private void SelectGoal(string selectedGoal)
        {
            Goal = selectedGoal;
            ValidationError = string.Empty;
        }

        [RelayCommand]
        private void ToggleHeightUnit()
        {
            UpdateRawHeightFromInputs();
            if (HeightUnit == "ft/in")
            {
                HeightUnit = "cm";
                double cm = _rawHeightInches * 2.54;
                HeightCmText = cm.ToString("F1");
            }
            else
            {
                HeightUnit = "ft/in";
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
            CalculateBmi();
        }

        [RelayCommand]
        private void ToggleWeightUnit()
        {
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
            CalculateBmi();
        }

        public void OnHeightInputChanged()
        {
            UpdateRawHeightFromInputs();
            UpdatePreviews();
            CalculateBmi();
        }

        public void OnWeightInputChanged()
        {
            UpdateRawWeightFromInputs();
            UpdatePreviews();
            CalculateBmi();
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

        private void CalculateBmi()
        {
            double heightMeters = 0;
            if (HeightUnit == "cm")
            {
                if (double.TryParse(HeightCmText, out double cmVal))
                {
                    heightMeters = cmVal / 100.0;
                }
            }
            else
            {
                double inches = _rawHeightInches;
                double cm = inches * 2.54;
                heightMeters = cm / 100.0;
            }

            double weightKg = _rawWeightKg;

            if (heightMeters > 0 && weightKg > 0)
            {
                double bmi = weightKg / (heightMeters * heightMeters);
                BmiText = bmi.ToString("F1");

                if (bmi < 17.0)
                {
                    BmiStatus = "Severe Thinness";
                    BmiBgColor = "#C62828";
                }
                else if (bmi < 18.5)
                {
                    BmiStatus = "Underweight";
                    BmiBgColor = "#EF6C00";
                }
                else if (bmi <= 24.9)
                {
                    BmiStatus = "Normal";
                    BmiBgColor = "#2E7D32";
                }
                else if (bmi < 30.0)
                {
                    BmiStatus = "Overweight";
                    BmiBgColor = "#EF6C00";
                }
                else
                {
                    BmiStatus = "Obese";
                    BmiBgColor = "#C62828";
                }
            }
            else
            {
                BmiText = "--";
                BmiStatus = string.Empty;
                BmiBgColor = "#2A2A2E";
            }
        }

        public void RecalculateSuggestedTargets()
        {
            UpdateRawHeightFromInputs();
            UpdateRawWeightFromInputs();

            double heightCm = HeightUnit == "cm" ? (_rawHeightInches * 2.54) : (_rawHeightInches * 2.54);
            if (HeightUnit == "cm" && double.TryParse(HeightCmText, out double parsedCm) && parsedCm > 0)
            {
                heightCm = parsedCm;
            }

            double weightKg = _rawWeightKg;
            int.TryParse(AgeText, out int age);

            double bmr = NutritionCalculator.CalculateBmr(weightKg, heightCm, age, Gender);
            double tdee = NutritionCalculator.CalculateTdee(bmr, ActivityLevel);
            double calories = NutritionCalculator.CalculateCalories(tdee, Goal);
            double water = NutritionCalculator.CalculateWater(weightKg);

            CalorieLimitText = calories.ToString("0.##");
            WaterLimitText = water.ToString("0.##");
        }

        [RelayCommand]
        private async Task GoNext()
        {
            ValidationError = string.Empty;

            if (CurrentStep == 1)
            {
                CurrentStep = 2;
            }
            else if (CurrentStep == 2)
            {
                if (string.IsNullOrEmpty(Gender))
                {
                    ValidationError = "Please select a gender option to proceed.";
                    return;
                }
                CurrentStep = 3;
            }
            else if (CurrentStep == 3)
            {
                if (!int.TryParse(AgeText, out int ageVal) || ageVal < 5 || ageVal > 120)
                {
                    ValidationError = "Please enter a valid age between 5 and 120.";
                    return;
                }
                CurrentStep = 4;
            }
            else if (CurrentStep == 4)
            {
                if (string.IsNullOrEmpty(ActivityLevel))
                {
                    ValidationError = "Please select an activity level option to proceed.";
                    return;
                }
                CurrentStep = 5;
            }
            else if (CurrentStep == 5)
            {
                UpdateRawHeightFromInputs();
                UpdateRawWeightFromInputs();

                if (HeightUnit == "cm")
                {
                    if (!double.TryParse(HeightCmText, out double cmVal) || cmVal <= 50 || cmVal > 300)
                    {
                        ValidationError = "Please enter a valid height in cm (50 to 300).";
                        return;
                    }
                }
                else
                {
                    if (!double.TryParse(HeightFeetText, out double ftVal) || ftVal < 1 || ftVal > 9 ||
                        !double.TryParse(HeightInchesText, out double inVal) || inVal < 0 || inVal >= 12)
                    {
                        ValidationError = "Please enter valid feet (1-9) and inches (0-11.9).";
                        return;
                    }
                }

                if (!double.TryParse(WeightValueText, out double weightVal) || weightVal <= 10 || weightVal > 600)
                {
                    ValidationError = $"Please enter a valid weight (10 to 600 {WeightUnit}).";
                    return;
                }

                CurrentStep = 6;
            }
            else if (CurrentStep == 6)
            {
                if (string.IsNullOrEmpty(Goal))
                {
                    ValidationError = "Please select a goal option to proceed.";
                    return;
                }
                RecalculateSuggestedTargets();
                CurrentStep = 7;
            }
            else if (CurrentStep == 7)
            {
                if (!double.TryParse(CalorieLimitText, out double calVal) || calVal <= 500 || calVal > 10000)
                {
                    ValidationError = "Calorie target must be between 500 and 10,000 kcal.";
                    return;
                }

                if (!double.TryParse(WaterLimitText, out double watVal) || watVal <= 100 || watVal > 20000)
                {
                    ValidationError = "Water target must be between 100 and 20,000 mL.";
                    return;
                }

                CurrentStep = 8;
            }
            else if (CurrentStep == 8)
            {
                await CompleteOnboarding();
            }
        }

        [RelayCommand]
        private void GoBackStep()
        {
            ValidationError = string.Empty;
            if (CurrentStep > 1)
            {
                CurrentStep--;
            }
        }

        [RelayCommand]
        private async Task CompleteOnboarding()
        {
            ValidationError = string.Empty;

            var isOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
            if (isOffline)
            {
                ValidationError = "Network disconnected. Could not save profile cloud setup.";
                return;
            }

            var user = _plannerStateService.CurrentUser;
            if (user == null)
            {
                ValidationError = "Session user missing.";
                return;
            }

            IsBusy = true;
            try
            {
                user.Gender = Gender;
                user.Age = int.Parse(AgeText);
                user.ActivityLevel = ActivityLevel;
                user.Goal = Goal;
                user.HeightUnit = HeightUnit;
                user.WeightUnit = WeightUnit;

                if (HeightUnit == "cm")
                {
                    user.HeightValue = double.Parse(HeightCmText);
                }
                else
                {
                    user.HeightValue = _rawHeightInches;
                }

                user.WeightValue = double.Parse(WeightValueText);
                user.CalorieLimit = double.Parse(CalorieLimitText);
                user.WaterLimit = double.Parse(WaterLimitText);

                var success = await _userRepository.UpdateUserAsync(user);
                if (success)
                {
                    _plannerStateService.IsOnboardingCompleted = true;
                    await NavigationService.GoToAsync("//HomePage");
                }
                else
                {
                    ValidationError = "Failed to update profile to backend database.";
                }
            }
            catch (Exception ex)
            {
                ValidationError = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
