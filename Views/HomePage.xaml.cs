using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
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
