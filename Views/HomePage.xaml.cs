using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly Services.IPlannerStateService _plannerStateService;

        public HomePage(HomeViewModel vm, Services.IPlannerStateService plannerStateService)
        {
            InitializeComponent();
            BindingContext = vm;
            _plannerStateService = plannerStateService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_plannerStateService.IsOnboardingCompleted)
            {
                await Shell.Current.GoToAsync("//OnboardingPage");
                return;
            }

            if (BindingContext is HomeViewModel vm)
            {
                vm.UpdateUserName();
                _ = vm.LoadTodayProgressAsync();
            }
        }

        private void OnHamburgerTapped(object? sender, TappedEventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }
}
