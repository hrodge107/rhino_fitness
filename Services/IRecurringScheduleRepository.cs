using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IRecurringScheduleRepository
    {
        Task<bool> CreateScheduleAsync(RecurringSchedule schedule);
        Task<List<RecurringSchedule>> GetActiveSchedulesAsync(int userId);
        Task<RecurringSchedule?> GetByIdAsync(int id);
        Task<bool> CancelScheduleAsync(int scheduleId);
        Task ExtendWindowAsync(int userId);
        Task<bool> DeleteInstanceAsync(int scheduledExerciseId, string scope);
        Task<bool> UpdateInstanceDateAsync(int scheduledExerciseId, DateTime newDate, string scope);
    }
}
