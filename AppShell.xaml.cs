using System.Windows.Input;

namespace FitnessApp
{
    public partial class AppShell : Shell
    {
        public ICommand LogoutCommand => new Command(async () => await ExecuteLogoutAsync());

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;

            // Register routes for navigation from ViewModels
            Routing.RegisterRoute("HomePage", typeof(Views.HomePage));
            Routing.RegisterRoute("PlannerPage", typeof(Views.PlannerPage));
            Routing.RegisterRoute("WorkoutsPage", typeof(Views.WorkoutsPage));
            Routing.RegisterRoute("NutritionPage", typeof(Views.NutritionPage));
            Routing.RegisterRoute("ExerciseListPage", typeof(Views.ExerciseListPage));
            Routing.RegisterRoute("ExercisePage", typeof(Views.ExercisePage));
            Routing.RegisterRoute("ProfilePage", typeof(Views.ProfilePage));
            Routing.RegisterRoute("ProfileEditPage", typeof(Views.ProfileEditPage));
            Routing.RegisterRoute("SignupPage", typeof(Views.SignupPage));
            Routing.RegisterRoute("OnboardingPage", typeof(Views.OnboardingPage));
            Routing.RegisterRoute("MealCategoryPage", typeof(Views.MealCategoryPage));
            Routing.RegisterRoute("RemindersPage", typeof(Views.RemindersPage));
        }

        private async Task ExecuteLogoutAsync()
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
            if (!confirm) return;

            var services = Handler?.MauiContext?.Services;
            if (services != null)
            {
                var sessionService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<Services.SessionService>(services);
                var plannerStateService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<Services.IPlannerStateService>(services);

                if (sessionService != null)
                {
                    await sessionService.ClearSessionAsync();
                }
                if (plannerStateService != null)
                {
                    plannerStateService.CurrentUser = null;
                }
            }

            await GoToAsync("//LoginPage");
        }
    }
}
