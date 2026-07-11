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

            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);

            var calendar = Java.Util.Calendar.Instance;
            calendar.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            calendar.Set(Java.Util.CalendarField.HourOfDay, reminder.Hour);
            calendar.Set(Java.Util.CalendarField.Minute, reminder.Minute);
            calendar.Set(Java.Util.CalendarField.Second, 0);

            if (calendar.TimeInMillis <= Java.Lang.JavaSystem.CurrentTimeMillis())
            {
                calendar.Add(Java.Util.CalendarField.DayOfMonth, 1);
            }

            alarmManager?.SetRepeating(
                AlarmType.RtcWakeup,
                calendar.TimeInMillis,
                AlarmManager.IntervalDay,
                pendingIntent);
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

            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
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
