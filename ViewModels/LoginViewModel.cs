using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Services;

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



        public LoginViewModel(
            INavigationService navigationService,
            IUserRepository userRepository,
            IPlannerStateService plannerStateService,
            SessionService sessionService) : base(navigationService)
        {
            _userRepository = userRepository;
            _plannerStateService = plannerStateService;
            _sessionService = sessionService;
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
                var user = await _userRepository.LoginAsync(Username, Password);
                if (user != null)
                {
                    _plannerStateService.CurrentUser = user;
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
                _plannerStateService.CurrentUser = activeUser;
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
