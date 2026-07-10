using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.Services
{
    public class NotificationScheduler : INotificationScheduler
    {
        public async Task<bool> CheckPermissionsAsync()
        {
            var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized || 
                   settings.AuthorizationStatus == UNAuthorizationStatus.Provisional;
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            var (granted, error) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
            return granted;
        }

        public Task OpenSettingsAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var url = new NSUrl(UIApplication.OpenSettingsUrlString);
                if (UIApplication.SharedApplication.CanOpenUrl(url))
                {
                    UIApplication.SharedApplication.OpenUrl(url, new NSDictionary(), null);
                }
            });
            return Task.CompletedTask;
        }

        public async Task ScheduleReminderAsync(Reminder reminder)
        {
            var content = new UNMutableNotificationContent
            {
                Title = $"{reminder.Category} Reminder",
                Body = GetMessageForCategory(reminder.Category),
                Sound = UNNotificationSound.Default
            };

            var dateComponents = new NSDateComponents
            {
                Hour = reminder.Hour,
                Minute = reminder.Minute
            };

            var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, true);
            var request = UNNotificationRequest.FromIdentifier(reminder.Id.ToString(), content, trigger);

            await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request).ConfigureAwait(false);
        }

        public Task CancelReminderAsync(Reminder reminder)
        {
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(new[] { reminder.Id.ToString() });
            return Task.CompletedTask;
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
