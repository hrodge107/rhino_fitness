using System.Threading.Tasks;
using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface INotificationScheduler
    {
        Task<bool> RequestPermissionsAsync();
        Task<bool> CheckPermissionsAsync();
        Task ScheduleReminderAsync(Reminder reminder);
        Task CancelReminderAsync(Reminder reminder);
        Task OpenSettingsAsync();
    }
}
