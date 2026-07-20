using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace FitnessApp.Services
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class NotificationReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            var category = intent.GetStringExtra("category") ?? "Fitness";
            var message = intent.GetStringExtra("message") ?? "Time for your daily log!";
            var reminderId = intent.GetIntExtra("reminder_id", 0);

            var channelId = "fitness_reminders_channel";
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Daily Reminders", NotificationImportance.High)
                {
                    Description = "Daily alerts to log meals, workouts and water"
                };
                notificationManager?.CreateNotificationChannel(channel);
            }

            if (context.PackageName == null) return;
            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName);
            if (launchIntent == null) return;
            
            launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(context, reminderId, launchIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var iconId = context.Resources?.GetIdentifier("appicon", "mipmap", context.PackageName) ?? 0;
            if (iconId == 0)
            {
                iconId = Android.Resource.Drawable.SymDefAppIcon;
            }

            var notificationBuilder = new NotificationCompat.Builder(context, channelId)
                .SetContentTitle($"{category} Reminder")
                .SetContentText(message)
                .SetSmallIcon(iconId)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetPriority(NotificationCompat.PriorityHigh);

            notificationManager?.Notify(reminderId, notificationBuilder.Build());

            if (reminderId > 0)
            {
                var pendingResult = GoAsync();
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var dbPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "fitnessapp.db3");
                        if (System.IO.File.Exists(dbPath))
                        {
                            var connection = new SQLite.SQLiteAsyncConnection(dbPath, SQLite.SQLiteOpenFlags.ReadWrite | SQLite.SQLiteOpenFlags.SharedCache, storeDateTimeAsTicks: false);
                            var reminder = await connection.Table<FitnessApp.Models.Reminder>().Where(r => r.Id == reminderId).FirstOrDefaultAsync().ConfigureAwait(false);
                            if (reminder != null)
                            {
                                NotificationScheduler.ScheduleNotification(context, reminder);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"NotificationReceiver failed to reschedule: {ex.Message}");
                    }
                    finally
                    {
                        pendingResult?.Finish();
                    }
                });
            }
        }
    }
}
