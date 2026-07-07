using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByNameAsync(string name);
        Task<User?> ValidateUserAsync(string usernameOrEmail, string password);
        Task<bool> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<User?> GetOrFetchUserAsync(string email);
        Task<bool> SaveUserAsync(User user);
        Task SyncPendingChangesAsync();
        Task RefreshActiveUserAsync();
        Task<User?> LoginAsync(string email, string rawPassword);
    }
}
