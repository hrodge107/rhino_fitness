namespace FitnessApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation from ViewModels
            Routing.RegisterRoute("HomePage", typeof(Views.HomePage));
            Routing.RegisterRoute("PlannerPage", typeof(Views.PlannerPage));
            Routing.RegisterRoute("WorkoutsPage", typeof(Views.WorkoutsPage));
            Routing.RegisterRoute("NutritionPage", typeof(Views.NutritionPage));
            Routing.RegisterRoute("ExerciseListPage", typeof(Views.ExerciseListPage));
            Routing.RegisterRoute("ExercisePage", typeof(Views.ExercisePage));
            Routing.RegisterRoute("ProfilePage", typeof(Views.ProfilePage));
            Routing.RegisterRoute("ProfileEditPage", typeof(Views.ProfileEditPage));
        }
    }
}
