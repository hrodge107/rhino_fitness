using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Services;
using Microsoft.Maui.Networking;

namespace FitnessApp.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlannerStateService _plannerStateService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isOffline;

        public LoginViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService,
            SessionService sessionService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
            _sessionService = sessionService;

            UpdateConnectivity();
            Connectivity.Current.ConnectivityChanged += (s, e) => UpdateConnectivity();
        }

        private void UpdateConnectivity()
        {
            IsOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
        }

        [RelayCommand]
        private async Task SignIn()
        {
            if (IsOffline)
            {
                await Shell.Current.DisplayAlert("Error", "No internet connection. Please connect and try again.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter username and password.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _userRepository.LoginAsync(Username, Password);
                if (user != null)
                {
                    _plannerStateService.CurrentUser = user;
                    _plannerStateService.IsOnboardingCompleted = true;
                    // Navigate to home
                    await NavigationService.GoToAsync("//HomePage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Invalid email or password.", "OK");
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

        public async Task CheckActiveSessionAsync()
        {
            var activeUser = await _sessionService.GetActiveUserAsync();
            if (activeUser != null)
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await _sessionService.ClearSessionAsync();
                    await Shell.Current.DisplayAlert("Connection Required", "An internet connection is required to restore your session.", "OK");
                    return;
                }
                _plannerStateService.CurrentUser = activeUser;
                _plannerStateService.IsOnboardingCompleted = true;
                await NavigationService.GoToAsync("//HomePage");
            }
        }

        [RelayCommand]
        private Task ForgotPassword()
        {
            // ponytail: mock forgot password action
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task NavigateToSignUp()
        {
            await NavigationService.GoToAsync("SignupPage");
        }
    }
}
