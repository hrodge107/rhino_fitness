using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using FitnessApp.Models;

namespace FitnessApp.Services
{
    public class ReminderRepository : IReminderRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        public ReminderRepository(IDatabaseService databaseService)
        {
            _connection = databaseService.Connection;
        }

        public async Task<List<Reminder>> GetRemindersForUserAsync(int userId)
        {
            var reminders = await _connection.Table<Reminder>()
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.Hour)
                .ThenBy(r => r.Minute)
                .ToListAsync();

            var now = DateTime.Now;
            var expiredReminders = reminders.Where(r => 
                string.Equals(r.RecurrenceType, "custom_interval", StringComparison.OrdinalIgnoreCase) && 
                r.EndDate.HasValue && 
                r.EndDate.Value < now).ToList();

            if (expiredReminders.Any())
            {
                foreach (var expired in expiredReminders)
                {
                    await _connection.DeleteAsync<Reminder>(expired.Id);
                    reminders.Remove(expired);
                }
            }

            return reminders;
        }

        public async Task<bool> AddReminderAsync(Reminder reminder)
        {
            reminder.CreatedAt = DateTime.UtcNow;
            var result = await _connection.InsertAsync(reminder);
            return result > 0;
        }

        public async Task<bool> DeleteReminderAsync(int reminderId)
        {
            var result = await _connection.DeleteAsync<Reminder>(reminderId);
            return result > 0;
        }
    }
}
