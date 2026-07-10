using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace FitnessApp.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IPlannerStateService _plannerStateService;
        private readonly SessionService _sessionService;
        private readonly Supabase.Client _supabaseClient;
        private CancellationTokenSource? _cancellationTokenSource;

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
        [NotifyPropertyChangedFor(nameof(IsOnline))]
        [NotifyPropertyChangedFor(nameof(ShowTimeline))]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
        [NotifyPropertyChangedFor(nameof(ShowOfflineState))]
        private bool _isOffline;

        public bool IsOnline => !IsOffline;

        public bool ShowTimeline => !IsOffline && Activities.Count > 0;
        public bool ShowEmptyState => !IsOffline && Activities.Count == 0;
        public bool ShowOfflineState => IsOffline;

        [ObservableProperty]
        private bool _loadMoreVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyStateText))]
        [NotifyPropertyChangedFor(nameof(EmptyStateSubtext))]
        private string _selectedFilter = "All";

        public string EmptyStateText => SelectedFilter switch
        {
            "Exercises" => "No workouts completed recently",
            "Meal" => "No meals logged recently",
            "Water" => "No water logged recently",
            _ => "No activities logged recently"
        };

        public string EmptyStateSubtext => SelectedFilter switch
        {
            "Exercises" => "Complete scheduled workouts in your planner to log exercise activity!",
            "Meal" => "Log your daily meals in the nutrition section to see them here!",
            "Water" => "Log your hydration in the nutrition section to track water intake!",
            _ => "Complete workouts or log nutrition to see your daily activity history!"
        };

        public List<string> GenderOptions { get; } = new()
        {
            "Male",
            "Female",
            "Other",
            "Prefer not to say"
        };

        public ObservableCollection<TimelineActivityItem> Activities { get; } = new();

        private int _limit = 10;

        public ProfileViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IExerciseRepository exerciseRepository,
            IPlannerStateService plannerStateService,
            SessionService sessionService,
            Supabase.Client supabaseClient) : base(navigationService)
        {
            _userRepository = userRepository;
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _exerciseRepository = exerciseRepository;
            _plannerStateService = plannerStateService;
            _sessionService = sessionService;
            _supabaseClient = supabaseClient;
            ActiveTab = "Profile";
        }

        private async Task<List<SupabaseScheduledExercise>> FetchSupabaseExercisesAsync(int userId, int limit)
        {
            var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                .Where(x => x.UserId == userId && x.Status == "COMPLETED")
                .Order(x => x.UpdatedAt, Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get()
                .ConfigureAwait(false);
            return result.Models;
        }

        private async Task<List<SupabaseMealLog>> FetchSupabaseMealsAsync(int userId, int limit)
        {
            var result = await _supabaseClient.From<SupabaseMealLog>()
                .Where(x => x.UserId == userId)
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get()
                .ConfigureAwait(false);
            return result.Models;
        }

        private async Task<List<SupabaseWaterLog>> FetchSupabaseWaterAsync(int userId, int limit)
        {
            var result = await _supabaseClient.From<SupabaseWaterLog>()
                .Where(x => x.UserId == userId)
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get()
                .ConfigureAwait(false);
            return result.Models;
        }

        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            IsBusy = true;
            try
            {
                IsOffline = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet;
                OnPropertyChanged(nameof(IsOnline));
            var user = _plannerStateService.CurrentUser;

            if (user != null)
            {
                Name = user.Name;
                Gender = string.IsNullOrWhiteSpace(user.Gender) ? string.Empty : user.Gender;
                Age = user.Age;

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
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCompletedExercisesAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null) return;

            // Connectivity Check
            IsOffline = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet;
            if (IsOffline)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Activities.Clear();
                    HasNoCompletedExercises = false;
                    LoadMoreVisible = false;
                    // Notify layout state changes
                    OnPropertyChanged(nameof(ShowTimeline));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowOfflineState));
                });
                return;
            }

            // Rapid switching cancelation token management
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                var combinedList = new List<TimelineActivityItem>();
                var geometryConverter = new Microsoft.Maui.Controls.Shapes.PathGeometryConverter();

                if (SelectedFilter == "All" || SelectedFilter == "Exercises")
                {
                    var exercises = await FetchSupabaseExercisesAsync(user.Id, _limit);
                    token.ThrowIfCancellationRequested();
                    foreach (var se in exercises)
                    {
                        var exercise = await _exerciseRepository.GetByExerciseIdAsync(se.ExerciseId);
                        token.ThrowIfCancellationRequested();
                        combinedList.Add(new TimelineActivityItem
                        {
                            Id = $"exercise_{se.Id}",
                            Type = "Exercise",
                            Title = exercise?.Name ?? "Exercise",
                            Subtitle = exercise?.BodyPart ?? "Workout",
                            Details = $"{se.Sets} sets • {se.DurationSeconds / 60:D2}:{se.DurationSeconds % 60:D2}",
                            Timestamp = se.UpdatedAt.Kind == DateTimeKind.Utc ? se.UpdatedAt.ToLocalTime() : se.UpdatedAt,
                            IconData = (Geometry)geometryConverter.ConvertFromInvariantString("M12,5c-1.7,0-3,1.3-3,3v2c-2.2,0.8-4,3-4,5.5C5,18.5,7.7,21,11,21h2c3.3,0,6-2.5,6-5.5c0-2.5-1.8-4.7-4-5.5V8C15,6.3,13.7,5,12,5z M12,7c0.6,0,1,0.4,1,1v1.2c-0.3-0.1-0.7-0.2-1-0.2s-0.7,0.1-1,0.2V8C11,7.4,11.4,7,12,7z")!,
                            ThemeColor = new SolidColorBrush(Color.FromArgb("#5B2A9E"))
                        });
                    }
                }

                if (SelectedFilter == "All" || SelectedFilter == "Meal")
                {
                    var meals = await FetchSupabaseMealsAsync(user.Id, _limit);
                    token.ThrowIfCancellationRequested();
                    foreach (var m in meals)
                    {
                        token.ThrowIfCancellationRequested();
                        combinedList.Add(new TimelineActivityItem
                        {
                            Id = $"meal_{m.Id}",
                            Type = "Meal",
                            Title = m.FoodName,
                            Subtitle = m.Category,
                            Details = $"{m.Calories:F0} kcal",
                            Timestamp = m.CreatedAt.Kind == DateTimeKind.Utc ? m.CreatedAt.ToLocalTime() : m.CreatedAt,
                            IconData = (Geometry)geometryConverter.ConvertFromInvariantString("M11,9 H9 V2 H7 v7 H5 V2 H3 v7 c0,2.12 1.66,3.87 3.75,3.97 V22 h2.5 v-8.03 c2.09,-0.1 3.75,-1.85 3.75,-3.97 V2 Z M16,6 v8 h3v8 h2.5V2 c-3.13,0 -5.5,2.5 -5.5,4 z")!,
                            ThemeColor = new SolidColorBrush(Color.FromArgb("#9B7FD4"))
                        });
                    }
                }

                if (SelectedFilter == "All" || SelectedFilter == "Water")
                {
                    var waters = await FetchSupabaseWaterAsync(user.Id, _limit);
                    token.ThrowIfCancellationRequested();
                    foreach (var w in waters)
                    {
                        token.ThrowIfCancellationRequested();
                        combinedList.Add(new TimelineActivityItem
                        {
                            Id = $"water_{w.Id}",
                            Type = "Water",
                            Title = "Hydration",
                            Subtitle = "Water Log",
                            Details = $"{w.Amount:F0} mL",
                            Timestamp = w.CreatedAt.Kind == DateTimeKind.Utc ? w.CreatedAt.ToLocalTime() : w.CreatedAt,
                            IconData = (Geometry)geometryConverter.ConvertFromInvariantString("M12,2.69 C12,2.69 19,10.43 19,15 C19,18.87 15.87,22 12,22 C8.13,22 5,18.87 5,15 C5,10.43 12,2.69 12,2.69 Z")!,
                            ThemeColor = new SolidColorBrush(Color.FromArgb("#7F9BD4"))
                        });
                    }
                }

                token.ThrowIfCancellationRequested();

                var sorted = combinedList
                    .OrderByDescending(a => a.Timestamp)
                    .Take(_limit)
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested) return;

                    Activities.Clear();
                    foreach (var activity in sorted)
                    {
                        Activities.Add(activity);
                    }

                    HasNoCompletedExercises = Activities.Count == 0;
                    LoadMoreVisible = combinedList.Count > sorted.Count;

                    // Trigger property changes to update layout states
                    OnPropertyChanged(nameof(ShowTimeline));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowOfflineState));
                });
            }
            catch (OperationCanceledException)
            {
                // Silently ignore cancellation from rapid switching
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load activities: {ex.Message}");
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

            IsOffline = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet;
            if (IsOffline)
            {
                ValidationError = "Cannot save profile changes while offline.";
                return;
            }

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
                IsBusy = true;
                try
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
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task NavigateToEditMetrics()
        {
            await NavigationService.GoToAsync("ProfileEditPage");
        }

        [RelayCommand]
        private async Task NavigateToReminders()
        {
            try
            {
                await NavigationService.GoToAsync("RemindersPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] Navigation to RemindersPage failed: {ex}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CRITICAL] InnerException: {ex.InnerException}");
                }
                throw;
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            IsBusy = true;
            try
            {
                _limit += 10;
                await LoadCompletedExercisesAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SetFilterAsync(string filter)
        {
            SelectedFilter = filter;
            _limit = 10; // Reset pagination limit on tab switch
            await LoadCompletedExercisesAsync();
        }

        [RelayCommand]
        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
            if (!confirm) return;

            await _sessionService.ClearSessionAsync();
            _plannerStateService.CurrentUser = null;
            await NavigationService.GoToAsync("//LoginPage");
        }
    }

    public class CompletedExerciseItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string Muscle { get; set; } = string.Empty;
        public int Sets { get; set; }
        public int DurationSeconds { get; set; }
        public string DurationText
        {
            get
            {
                int minutes = DurationSeconds / 60;
                int seconds = DurationSeconds % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }
        public string DetailsText => $"{Sets} sets • {DurationText}";
    }
}
