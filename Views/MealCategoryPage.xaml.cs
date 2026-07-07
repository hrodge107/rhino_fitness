using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class MealCategoryPage : ContentPage
    {
        private readonly MealCategoryViewModel _vm;

        public MealCategoryPage(MealCategoryViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadLogsAsync();
        }
    }
}
