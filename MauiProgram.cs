using FitnessApp.Services;
using FitnessApp.ViewModels;
using FitnessApp.Views;
using Microsoft.Extensions.Logging;

namespace FitnessApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Syne-Bold.ttf", "SyneBold");
                });

            // Infrastructure singletons — one connection, one catalog, app lifetime.
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IScheduledExerciseRepository, ScheduledExerciseRepository>();
            builder.Services.AddSingleton<IPlannerStateService, PlannerStateService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // Views and ViewModels
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<PlannerPage>();
            builder.Services.AddTransient<PlannerViewModel>();
            builder.Services.AddTransient<WorkoutsPage>();
            builder.Services.AddTransient<NutritionPage>();
            builder.Services.AddTransient<ExerciseListPage>();
            builder.Services.AddTransient<ExerciseListViewModel>();
            builder.Services.AddTransient<ExercisePage>();
            builder.Services.AddTransient<ExercisePageViewModel>();
            builder.Services.AddTransient<ExerciseCatalogViewModel>();
            builder.Services.AddTransient<WorkoutViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ProfileEditPage>();
            builder.Services.AddTransient<ProfileEditViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Seed the offline catalog off the UI thread. Fire-and-forget so app
            // startup is never blocked; failures degrade to an empty catalog rather
            // than crashing the launch (logged for diagnosis).
            _ = Task.Run(async () =>
            {
                try
                {
                    var db = app.Services.GetRequiredService<IDatabaseService>();
                    await db.SeedAsync();
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger(nameof(MauiProgram));
                    logger?.LogError(ex, "Offline catalog seed failed.");
                }
            });

            return app;
        }
    }
}
