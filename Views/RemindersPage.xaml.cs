using Microsoft.Maui.Controls;
using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class RemindersPage : ContentPage
    {
        private readonly RemindersViewModel _viewModel;
        private bool _isFirstAppearance = true;

        public RemindersPage(RemindersViewModel viewModel)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] RemindersPage InitializeComponent failed: {ex}");
                throw;
            }
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Window != null)
            {
                Window.Activated += OnWindowActivated;
            }

            try
            {
                if (_isFirstAppearance)
                {
                    _isFirstAppearance = false;
                    await _viewModel.InitializeAsync();
                }
                else
                {
                    await _viewModel.CheckPermissionsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] RemindersPage OnAppearing failed: {ex}");
                throw;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Window != null)
            {
                Window.Activated -= OnWindowActivated;
            }
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.CheckPermissionsAsync();
            });
        }
    }
}
