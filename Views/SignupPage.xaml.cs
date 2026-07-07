using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class SignupPage : ContentPage
    {
        public SignupPage(SignupViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
