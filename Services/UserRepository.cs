using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        public UserRepository(IDatabaseService database)
        {
            _connection = database.Connection;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _connection.Table<User>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var target = email.Trim().ToLowerInvariant();
            return await _connection.Table<User>()
                .Where(u => u.Email.ToLower() == target)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<User?> GetByNameAsync(string name)
        {
            var target = name.Trim().ToLowerInvariant();
            return await _connection.Table<User>()
                .Where(u => u.Name.ToLower() == target)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<User?> ValidateUserAsync(string usernameOrEmail, string password)
        {
            var target = usernameOrEmail.Trim().ToLowerInvariant();
            
            // Find user by username/name or email, then check password.
            // ponytail: simple plain-text comparison since it is a local app
            var user = await _connection.Table<User>()
                .Where(u => u.Email.ToLower() == target || u.Name.ToLower() == target)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (user != null && user.Password == password)
            {
                return user;
            }

            return null;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            // Set sync metadata
            user.IsSynced = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _connection.InsertAsync(user).ConfigureAwait(false);
            return result > 0;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            user.IsSynced = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _connection.UpdateAsync(user).ConfigureAwait(false);
            return result > 0;
        }
    }
}
