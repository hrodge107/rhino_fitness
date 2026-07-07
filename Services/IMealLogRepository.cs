using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IMealLogRepository
    {
        Task<List<MealLog>> GetMealLogsForDateAsync(int userId, string date);
        Task<bool> AddMealLogAsync(MealLog log);
        Task<bool> UpdateMealLogAsync(MealLog log);
        Task<bool> DeleteMealLogAsync(int id);
        Task<double> GetDailyCaloriesAsync(int userId, string date);
        Task<double> GetCategoryCaloriesAsync(int userId, string date, string category);
        Task SyncPendingLogsAsync(int userId);
    }
}
