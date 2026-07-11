using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class OnboardingPage : ContentPage
    {
        private readonly OnboardingViewModel _vm;

        public OnboardingPage(OnboardingViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        private void OnHeightTextChanged(object? sender, TextChangedEventArgs e)
        {
            _vm.OnHeightInputChanged();
        }

        private void OnWeightTextChanged(object? sender, TextChangedEventArgs e)
        {
            _vm.OnWeightInputChanged();
        }

        protected override bool OnBackButtonPressed()
        {
            // Block hardware back button during mandatory onboarding
            return true;
        }
    }
}
