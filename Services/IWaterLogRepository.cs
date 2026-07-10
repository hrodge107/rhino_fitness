using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IWaterLogRepository
    {
        Task<List<WaterLog>> GetWaterLogsForDateAsync(int userId, string date);
        Task<bool> AddWaterLogAsync(WaterLog log);
        Task<bool> DeleteWaterLogAsync(int id);
    }
}
