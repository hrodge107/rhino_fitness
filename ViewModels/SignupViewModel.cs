using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class SignupViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _validationError = string.Empty;

        public SignupViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
        }

        [RelayCommand]
        private async Task SignUp()
        {
            ValidationError = string.Empty;

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ValidationError = "All fields are required.";
                return;
            }

            if (!Email.Contains("@") || !Email.Contains("."))
            {
                ValidationError = "Please enter a valid email address.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ValidationError = "Passwords do not match.";
                return;
            }

            if (Password.Length < 6)
            {
                ValidationError = "Password must be at least 6 characters.";
                return;
            }

            IsBusy = true;
            try
            {
                // Check if username already exists
                var existingUserByName = await _userRepository.GetByNameAsync(Name);
                if (existingUserByName != null)
                {
                    ValidationError = "Username is already taken.";
                    return;
                }

                // Check if email already exists
                var existingUserByEmail = await _userRepository.GetByEmailAsync(Email);
                if (existingUserByEmail != null)
                {
                    ValidationError = "Email is already registered.";
                    return;
                }

                var newUser = new User
                {
                    Name = Name.Trim(),
                    Email = Email.Trim().ToLowerInvariant(),
                    Password = BCrypt.Net.BCrypt.HashPassword(Password),
                    IsSynced = false,
                    UpdatedAt = DateTime.UtcNow,
                    Gender = string.Empty,
                    Age = 0,
                    HeightValue = 0,
                    WeightValue = 0
                };

                var created = await _userRepository.CreateUserAsync(newUser);
                if (created)
                {
                    _plannerStateService.CurrentUser = newUser;
                    await NavigationService.GoToAsync("//HomePage");
                }
                else
                {
                    ValidationError = "Registration failed. Please try again.";
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

        [RelayCommand]
        private async Task BackToLogin()
        {
            await NavigationService.GoBackAsync();
        }
    }
}
