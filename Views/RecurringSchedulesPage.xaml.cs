using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class RecurringSchedulesPage : ContentPage
    {
        private readonly RecurringSchedulesViewModel _viewModel;

        public RecurringSchedulesPage(RecurringSchedulesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadSchedulesAsync();
        }
    }
}
