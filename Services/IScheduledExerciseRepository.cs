using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IScheduledExerciseRepository
    {
        Task<List<ScheduledExercise>> GetScheduledExercisesForDateAsync(int userId, DateTime date);
        Task<bool> AddScheduledExercisesAsync(IEnumerable<ScheduledExercise> exercises);
        Task<bool> DeleteScheduledExerciseAsync(int id);
        Task<int> UpdateMissedExercisesAsync(int userId, DateTime today);
        Task<List<ScheduledExercise>> GetCompletedExercisesAsync(int userId, DateTime startDate, DateTime endDate);
        Task<List<ScheduledExercise>> GetScheduledExercisesForRangeAsync(int userId, DateTime startDate, DateTime endDate);
        Task<ScheduledExercise?> GetByIdAsync(int id);
        Task<bool> UpdateScheduledExerciseAsync(ScheduledExercise exercise);
        Task<bool> HasOlderCompletedExercisesAsync(int userId, DateTime startDate);
    }
}
