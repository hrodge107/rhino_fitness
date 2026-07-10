using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IReminderRepository
    {
        Task<List<Reminder>> GetRemindersForUserAsync(int userId);
        Task<bool> AddReminderAsync(Reminder reminder);
        Task<bool> DeleteReminderAsync(int reminderId);
    }
}
