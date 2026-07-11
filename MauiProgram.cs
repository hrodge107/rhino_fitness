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
            builder.Services.AddSingleton<SessionService>(provider => 
                new SessionService(provider.GetRequiredService<IDatabaseService>().Connection));
            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IScheduledExerciseRepository, ScheduledExerciseRepository>();
            builder.Services.AddSingleton<IMealLogRepository, MealLogRepository>();
            builder.Services.AddSingleton<IWaterLogRepository, WaterLogRepository>();
            builder.Services.AddSingleton<IReminderRepository, ReminderRepository>();
            builder.Services.AddSingleton<IPlannerStateService, PlannerStateService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<INotificationScheduler, NotificationScheduler>();

            // Supabase Client registration
            builder.Services.AddSingleton(provider =>
            {
                var options = new Supabase.SupabaseOptions
                {
                    AutoConnectRealtime = false,
                    AutoRefreshToken = true
                };
                return new Supabase.Client(SupabaseConfig.Url, SupabaseConfig.AnonKey, options);
            });

            // Views and ViewModels
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<PlannerPage>();
            builder.Services.AddTransient<PlannerViewModel>();
            builder.Services.AddTransient<WorkoutsPage>();
            builder.Services.AddTransient<NutritionPage>();
            builder.Services.AddTransient<NutritionViewModel>();
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
            builder.Services.AddTransient<SignupPage>();
            builder.Services.AddTransient<SignupViewModel>();
            builder.Services.AddTransient<OnboardingPage>();
            builder.Services.AddTransient<OnboardingViewModel>();
            builder.Services.AddTransient<MealCategoryPage>();
            builder.Services.AddTransient<MealCategoryViewModel>();
            builder.Services.AddTransient<RemindersPage>();
            builder.Services.AddTransient<RemindersViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Seed and initialize off the UI thread. Fire-and-forget so app
            // startup is never blocked.
            _ = Task.Run(async () =>
            {
                try
                {
                    var dbService = app.Services.GetRequiredService<IDatabaseService>();
                    await dbService.SeedAsync();

                    var userRepository = app.Services.GetRequiredService<IUserRepository>();

                    // Sync if online
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        try
                        {
                            await userRepository.SyncPendingChangesAsync();
                            await userRepository.RefreshActiveUserAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Sync failed on launch: {ex.Message}");
                        }
                    }

                    // Restore active session
                    var sessionService = app.Services.GetRequiredService<SessionService>();
                    var activeUser = await sessionService.GetActiveUserAsync();
                    if (activeUser != null)
                    {
                        var plannerStateService = app.Services.GetRequiredService<IPlannerStateService>();
                        plannerStateService.CurrentUser = activeUser;
                    }
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger(nameof(MauiProgram));
                    logger?.LogError(ex, "App launch database initialization/sync failed.");
                }
            });

            return app;
        }
    }
}
