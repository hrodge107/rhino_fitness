using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using AndroidX.Core.Content;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.Services
{
    public class NotificationScheduler : INotificationScheduler
    {
        public async Task<bool> CheckPermissionsAsync()
        {
            var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
            return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            var status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
            return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
        }

        public Task OpenSettingsAsync()
        {
            try
            {
                var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                var uri = Android.Net.Uri.FromParts("package", Microsoft.Maui.ApplicationModel.AppInfo.PackageName, null);
                intent.SetData(uri);
                intent.AddFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch
            {
                var intent = new Intent(Android.Provider.Settings.ActionSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            return Task.CompletedTask;
        }

        public Task ScheduleReminderAsync(Reminder reminder)
        {
            ScheduleNotification(Android.App.Application.Context, reminder);
            return Task.CompletedTask;
        }

        public Task CancelReminderAsync(Reminder reminder)
        {
            CancelNotification(Android.App.Application.Context, reminder);
            return Task.CompletedTask;
        }

        public static void ScheduleNotification(Context context, Reminder reminder)
        {
            if (context == null || reminder == null) return;

            var intent = new Intent(context, typeof(NotificationReceiver));
            intent.PutExtra("category", reminder.Category);
            intent.PutExtra("message", GetMessageForCategory(reminder.Category));
            intent.PutExtra("reminder_id", reminder.Id);

            var pendingIntent = PendingIntent.GetBroadcast(
                context,
                reminder.Id,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            if (pendingIntent == null) return;

            var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;

            var nextRun = GetNextRunTime(reminder);
            if (!nextRun.HasValue) return;

            var targetCal = Java.Util.Calendar.Instance;
            if (targetCal == null) return;
            targetCal.Set(nextRun.Value.Year, nextRun.Value.Month - 1, nextRun.Value.Day, nextRun.Value.Hour, nextRun.Value.Minute, 0);
            targetCal.Set(Java.Util.CalendarField.Millisecond, 0);

            if (alarmManager != null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !alarmManager.CanScheduleExactAlarms())
                {
                    alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, targetCal.TimeInMillis, pendingIntent);
                }
                else
                {
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, targetCal.TimeInMillis, pendingIntent);
                }
            }
        }

        private static DateTime? GetNextRunTime(Reminder reminder)
        {
            var now = DateTime.Now;

            if (string.Equals(reminder.RecurrenceType, "custom_interval", StringComparison.OrdinalIgnoreCase))
            {
                if (reminder.EndDate.HasValue && reminder.EndDate.Value < now)
                {
                    return null; // Expired
                }

                DateTime start = reminder.StartDate ?? now.Date;
                DateTime nextRun = new DateTime(start.Year, start.Month, start.Day, reminder.Hour, reminder.Minute, 0);

                while (nextRun <= now)
                {
                    if (string.Equals(reminder.IntervalUnit, "weeks", StringComparison.OrdinalIgnoreCase))
                        nextRun = nextRun.AddDays(7 * Math.Max(1, reminder.IntervalValue));
                    else
                        nextRun = nextRun.AddDays(Math.Max(1, reminder.IntervalValue));
                }

                if (reminder.EndDate.HasValue && nextRun > reminder.EndDate.Value)
                {
                    return null; // Next run past end date
                }

                return nextRun;
            }
            else if (string.Equals(reminder.RecurrenceType, "weekly", StringComparison.OrdinalIgnoreCase))
            {
                var targetDays = ParseDaysOfWeek(reminder.DaysOfWeek);
                DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, reminder.Hour, reminder.Minute, 0);
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                if (targetDays.Count > 0)
                {
                    int attempts = 0;
                    while (!targetDays.Contains(nextRun.DayOfWeek) && attempts < 14)
                    {
                        nextRun = nextRun.AddDays(1);
                        attempts++;
                    }
                }

                return nextRun;
            }
            else // "daily" or default
            {
                DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, reminder.Hour, reminder.Minute, 0);
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }
                return nextRun;
            }
        }

        private static System.Collections.Generic.HashSet<DayOfWeek> ParseDaysOfWeek(string? daysOfWeekStr)
        {
            var result = new System.Collections.Generic.HashSet<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(daysOfWeekStr)) return result;

            var parts = daysOfWeekStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (Enum.TryParse<DayOfWeek>(part, true, out var dow))
                {
                    result.Add(dow);
                }
                else if (int.TryParse(part, out var num))
                {
                    if (num >= 0 && num <= 6)
                    {
                        result.Add((DayOfWeek)num);
                    }
                    else if (num == 7)
                    {
                        result.Add(DayOfWeek.Sunday);
                    }
                }
                else
                {
                    var match = part.ToLowerInvariant() switch
                    {
                        "sun" or "sunday" => DayOfWeek.Sunday,
                        "mon" or "monday" => DayOfWeek.Monday,
                        "tue" or "tues" or "tuesday" => DayOfWeek.Tuesday,
                        "wed" or "wednesday" => DayOfWeek.Wednesday,
                        "thu" or "thur" or "thurs" or "thursday" => DayOfWeek.Thursday,
                        "fri" or "friday" => DayOfWeek.Friday,
                        "sat" or "saturday" => DayOfWeek.Saturday,
                        _ => (DayOfWeek?)null
                    };
                    if (match.HasValue) result.Add(match.Value);
                }
            }
            return result;
        }

        public static void CancelNotification(Context context, Reminder reminder)
        {
            if (context == null || reminder == null) return;

            var intent = new Intent(context, typeof(NotificationReceiver));
            var pendingIntent = PendingIntent.GetBroadcast(
                context,
                reminder.Id,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            if (pendingIntent == null) return;

            var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            alarmManager?.Cancel(pendingIntent);
        }

        private static string GetMessageForCategory(string category)
        {
            return category switch
            {
                "Exercise" => "Time to get moving! Let's log your workout for today.",
                "Breakfast" => "Fuel your day! Don't forget to log your breakfast.",
                "Lunch" => "Midday energy boost! Log your lunch now.",
                "Dinner" => "Time to wrap up the day. Remember to log your dinner.",
                "Water" => "Stay hydrated! Keep tracking your water intake.",
                "Snack" => "Snack time! Log your snacks to stay on track.",
                _ => "Don't forget to log your daily fitness stats!"
            };
        }
    }
}
