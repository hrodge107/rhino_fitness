using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using SQLite;
using FitnessApp.Models;

namespace FitnessApp.Services
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;
            if (intent.Action != Intent.ActionBootCompleted) return;

            // We schedule on a background thread because database and AlarmManager setup involves disk I/O
            var pendingResult = GoAsync();
            Task.Run(async () =>
            {
                try
                {
                    var dbPath = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "fitnessapp.db3");
                    if (File.Exists(dbPath))
                    {
                        var connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache, storeDateTimeAsTicks: false);
                        var reminders = await connection.Table<Reminder>().ToListAsync().ConfigureAwait(false);

                        foreach (var reminder in reminders)
                        {
                            NotificationScheduler.ScheduleNotification(context, reminder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BootReceiver failed to reschedule alarms: {ex.Message}");
                }
                finally
                {
                    pendingResult.Finish();
                }
            });
        }
    }
}
