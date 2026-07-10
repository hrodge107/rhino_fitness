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
            return await _connection.Table<Reminder>()
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.Hour)
                .ThenBy(r => r.Minute)
                .ToListAsync();
        }

        public async Task<bool> AddReminderAsync(Reminder reminder)
        {
            // Check duplicate
            var duplicate = await _connection.Table<Reminder>()
                .Where(r => r.UserId == reminder.UserId &&
                            r.Category == reminder.Category &&
                            r.Hour == reminder.Hour &&
                            r.Minute == reminder.Minute)
                .FirstOrDefaultAsync();

            if (duplicate != null)
            {
                return false;
            }

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
