using System.Threading.Tasks;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.Services
{
    public class NotificationScheduler : INotificationScheduler
    {
        public Task<bool> CheckPermissionsAsync() => Task.FromResult(true);
        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);
        public Task OpenSettingsAsync() => Task.CompletedTask;
        public Task ScheduleReminderAsync(Reminder reminder) => Task.CompletedTask;
        public Task CancelReminderAsync(Reminder reminder) => Task.CompletedTask;
    }
}
