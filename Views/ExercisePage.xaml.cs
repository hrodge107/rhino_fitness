using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class ExercisePage : ContentPage
    {
        private readonly ExercisePageViewModel _viewModel;

        public ExercisePage(ExercisePageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.CleanUp();
        }
    }
}
