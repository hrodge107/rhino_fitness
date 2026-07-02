using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        public ProfileViewModel(INavigationService navigationService) : base(navigationService)
        {
            ActiveTab = "Profile";
        }
    }
}
