using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class ProfileEditPage : ContentPage
    {
        private readonly ProfileEditViewModel _vm;

        public ProfileEditPage(ProfileEditViewModel vm)
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
    }
}
