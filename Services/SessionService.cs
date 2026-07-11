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
            await WipeUserCachesAsync(user.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Wipes the local SQLite caches for user logs.
        /// </summary>
        public async Task WipeUserCachesAsync(int userId)
        {
            await _db.ExecuteAsync("DELETE FROM meal_logs WHERE user_id = ?", userId).ConfigureAwait(false);
            await _db.ExecuteAsync("DELETE FROM water_logs WHERE user_id = ?", userId).ConfigureAwait(false);
            await _db.ExecuteAsync("DELETE FROM scheduled_exercises WHERE user_id = ?", userId).ConfigureAwait(false);
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
