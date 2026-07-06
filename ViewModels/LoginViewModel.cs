using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public LoginViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
        }

        [RelayCommand]
        private async Task SignIn()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter username and password.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _userRepository.ValidateUserAsync(Username, Password);
                if (user != null)
                {
                    _plannerStateService.CurrentUser = user;
                    // Navigate to home
                    await NavigationService.GoToAsync("//HomePage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Invalid username or password.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Login error: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task ForgotPassword()
        {
            // ponytail: mock forgot password action
            return Task.CompletedTask;
        }
    }
}
