using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    public class SessionService
    {
        private readonly SQLiteAsyncConnection _db;

        public SessionService(SQLiteAsyncConnection db) { _db = db; }

        /// <summary>
        /// Sets the given user as active, deactivates all others.
        /// </summary>
        public async Task SetActiveUserAsync(User user)
        {
            await _db.ExecuteAsync("UPDATE users SET is_active = 0").ConfigureAwait(false);
            user.IsActive = true;
            await _db.UpdateAsync(user).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the currently active user, or null if none.
        /// </summary>
        public async Task<User?> GetActiveUserAsync()
        {
            return await _db.Table<User>()
                .Where(u => u.IsActive)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Clears session — sets all users to inactive.
        /// </summary>
        public async Task ClearSessionAsync()
        {
            await _db.ExecuteAsync("UPDATE users SET is_active = 0").ConfigureAwait(false);
        }
    }
}
