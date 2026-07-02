using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class WorkoutsPage : ContentPage
    {
        private readonly WorkoutViewModel _vm;

        public WorkoutsPage(WorkoutViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadBodyPartsAsync();
        }
    }
}
