using CommunityToolkit.Mvvm.Input;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        public LoginViewModel(INavigationService navigationService) : base(navigationService)
        {
        }

        [RelayCommand]
        private async Task SignIn()
        {
            // ponytail: mock login, navigate straight to home
            await NavigationService.GoToAsync("//HomePage");
        }

        [RelayCommand]
        private Task ForgotPassword()
        {
            // ponytail: mock forgot password action
            return Task.CompletedTask;
        }
    }
}
