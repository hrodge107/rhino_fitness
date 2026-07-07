using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Services;
using System.Threading.Tasks;

namespace FitnessApp.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        protected readonly INavigationService NavigationService;

        public BaseViewModel(INavigationService navigationService)
        {
            NavigationService = navigationService;
        }

        [ObservableProperty]
        private string _activeTab = string.Empty;

        [ObservableProperty]
        private bool _hasSearch = false;

        [ObservableProperty]
        private bool _isBusy;

        [RelayCommand]
        protected async Task NavigateToTab(string tabRoute)
        {
            await NavigationService.GoToAsync(tabRoute);
        }

        [RelayCommand]
        protected async Task GoBack()
        {
            await NavigationService.GoBackAsync();
        }
    }
}
