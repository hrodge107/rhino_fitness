using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Initials))]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _gender = string.Empty;

        [ObservableProperty]
        private int _age;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return "?";
                var parts = Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return "?";
                if (parts.Length == 1)
                    return parts[0][0].ToString().ToUpper();
                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditing))]
        private bool _isEditing;

        public bool IsNotEditing => !IsEditing;

        // Edit fields
        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editGender = string.Empty;

        [ObservableProperty]
        private string _editAgeText = string.Empty;

        [ObservableProperty]
        private string _validationError = string.Empty;

        [ObservableProperty]
        private string _heightText = "--";

        [ObservableProperty]
        private string _weightText = "--";

        [ObservableProperty]
        private string _bmiText = "--";

        [ObservableProperty]
        private string _bmiStatus = string.Empty;

        [ObservableProperty]
        private string _bmiBgColor = "#2A2A2E";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCompletedExercises))]
        private bool _hasNoCompletedExercises = true;

        public bool HasCompletedExercises => !HasNoCompletedExercises;

        [ObservableProperty]
        private bool _loadMoreVisible;

        public List<string> GenderOptions { get; } = new()
        {
            "Male",
            "Female",
            "Other",
            "Prefer not to say"
        };

        public ObservableCollection<CompletedExerciseItem> CompletedExercises { get; } = new();

        private int _loadedDays = 7;

        public ProfileViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IExerciseRepository exerciseRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _userRepository = userRepository;
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _exerciseRepository = exerciseRepository;
            _plannerStateService = plannerStateService;
            ActiveTab = "Profile";
        }

        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null)
            {
                // Fallback to Adam if no active user in session
                user = await _userRepository.GetByNameAsync("Adam");
                if (user != null)
                {
                    _plannerStateService.CurrentUser = user;
                }
            }

            if (user != null)
            {
                Name = user.Name;
                Gender = string.IsNullOrWhiteSpace(user.Gender) ? "Male" : user.Gender;
                Age = user.Age > 0 ? user.Age : 25;

                // Format height display
                if (user.HeightUnit == "cm")
                {
                    HeightText = $"{user.HeightValue:F1} cm";
                }
                else
                {
                    int feet = (int)(user.HeightValue / 12);
                    int inches = (int)Math.Round(user.HeightValue % 12);
                    if (inches == 12)
                    {
                        feet++;
                        inches = 0;
                    }
                    HeightText = $"{feet}'{inches}\"";
                }

                // Format weight display
                WeightText = $"{user.WeightValue:F1} {user.WeightUnit}";

                // Calculate BMI
                double heightMeters = 0;
                if (user.HeightUnit == "cm")
                {
                    heightMeters = user.HeightValue / 100.0;
                }
                else
                {
                    double cm = user.HeightValue * 2.54;
                    heightMeters = cm / 100.0;
                }

                double weightKg = 0;
                if (user.WeightUnit == "kg")
                {
                    weightKg = user.WeightValue;
                }
                else
                {
                    weightKg = user.WeightValue / 2.20462262185;
                }

                if (heightMeters > 0)
                {
                    double bmi = weightKg / (heightMeters * heightMeters);
                    BmiText = bmi.ToString("F1");

                    if (bmi < 17.0)
                    {
                        BmiStatus = "Severe Thinness";
                        BmiBgColor = "#C62828"; // Red (Severe Thinness)
                    }
                    else if (bmi < 18.5)
                    {
                        BmiStatus = "Underweight";
                        BmiBgColor = "#EF6C00"; // Orange (Mild Thinness)
                    }
                    else if (bmi <= 24.9)
                    {
                        BmiStatus = "Normal";
                        BmiBgColor = "#2E7D32"; // Green (Healthy)
                    }
                    else if (bmi < 30.0)
                    {
                        BmiStatus = "Overweight";
                        BmiBgColor = "#EF6C00"; // Orange
                    }
                    else
                    {
                        BmiStatus = "Obese";
                        BmiBgColor = "#C62828"; // Red
                    }
                }
                else
                {
                    BmiText = "--";
                    BmiStatus = string.Empty;
                    BmiBgColor = "#2A2A2E";
                }

                // Load completed exercises
                await LoadCompletedExercisesAsync();
            }
        }

        private async Task LoadCompletedExercisesAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null) return;

            DateTime endDate = DateTime.Today;
            DateTime startDate = DateTime.Today.AddDays(-_loadedDays + 1);

            var completed = await _scheduledExerciseRepository.GetCompletedExercisesAsync(user.Id, startDate, endDate);
            
            CompletedExercises.Clear();
            foreach (var se in completed)
            {
                var exercise = await _exerciseRepository.GetByExerciseIdAsync(se.ExerciseId);
                CompletedExercises.Add(new CompletedExerciseItem
                {
                    Name = exercise?.Name ?? "Exercise",
                    Category = exercise?.BodyPart ?? "Workout",
                    Muscle = exercise?.Muscle ?? "",
                    DateText = se.ScheduledDate.ToString("MMMM d, yyyy")
                });
            }

            HasNoCompletedExercises = CompletedExercises.Count == 0;

            // Check if there are older completed exercises to show "Load More"
            // ponytail: simple DB check for historical records before startDate
            var dbService = App.Current?.Handler?.MauiContext?.Services?.GetService(typeof(IDatabaseService)) as IDatabaseService;
            if (dbService != null)
            {
                var conn = dbService.Connection;
                var older = await conn.Table<ScheduledExercise>()
                    .Where(se => se.UserId == user.Id && se.Status == "COMPLETED" && se.ScheduledDate < startDate)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                
                // Marshal back to UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadMoreVisible = older != null;
                });
            }
            else
            {
                LoadMoreVisible = false;
            }
        }

        [RelayCommand]
        private void ToggleEdit()
        {
            if (!IsEditing)
            {
                EditName = Name;
                EditGender = Gender;
                EditAgeText = Age.ToString();
                ValidationError = string.Empty;
                IsEditing = true;
            }
            else
            {
                IsEditing = false;
            }
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            ValidationError = string.Empty;

            if (string.IsNullOrWhiteSpace(EditName))
            {
                ValidationError = "Name cannot be empty.";
                return;
            }

            if (EditName.Length > 50)
            {
                ValidationError = "Name must be 50 characters or less.";
                return;
            }

            if (!int.TryParse(EditAgeText, out int ageVal) || ageVal < 1 || ageVal > 120)
            {
                ValidationError = "Age must be between 1 and 120.";
                return;
            }

            var user = _plannerStateService.CurrentUser;
            if (user != null)
            {
                user.Name = EditName.Trim();
                user.Gender = EditGender;
                user.Age = ageVal;

                var success = await _userRepository.UpdateUserAsync(user);
                if (success)
                {
                    Name = user.Name;
                    Gender = user.Gender;
                    Age = user.Age;
                    IsEditing = false;
                }
                else
                {
                    ValidationError = "Failed to update profile in database.";
                }
            }
        }

        [RelayCommand]
        private async Task NavigateToEditMetrics()
        {
            await NavigationService.GoToAsync("ProfileEditPage");
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            _loadedDays += 7;
            await LoadCompletedExercisesAsync();
        }
    }

    public class CompletedExerciseItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string Muscle { get; set; } = string.Empty;
    }
}
