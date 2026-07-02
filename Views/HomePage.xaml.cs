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

        private void OnHamburgerTapped(object? sender, TappedEventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }
}
