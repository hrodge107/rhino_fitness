using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class HistoryViewModel : BaseViewModel
    {
        public HistoryViewModel(INavigationService navigationService) : base(navigationService)
        {
            ActiveTab = "History";
        }
    }
}
