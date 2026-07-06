using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    public class ScheduledExerciseRepository : IScheduledExerciseRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        public ScheduledExerciseRepository(IDatabaseService database)
        {
            _connection = database.Connection;
        }

        public async Task<List<ScheduledExercise>> GetScheduledExercisesForDateAsync(int userId, DateTime date)
        {
            var dateOnly = date.Date;
            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.ScheduledDate == dateOnly)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> AddScheduledExercisesAsync(IEnumerable<ScheduledExercise> exercises)
        {
            var success = true;
            await _connection.RunInTransactionAsync(syncConn =>
            {
                foreach (var exercise in exercises)
                {
                    exercise.ScheduledDate = exercise.ScheduledDate.Date;
                    exercise.IsSynced = false;
                    exercise.UpdatedAt = DateTime.UtcNow;
                    var rows = syncConn.Insert(exercise);
                    if (rows <= 0)
                    {
                        success = false;
                    }
                }
            }).ConfigureAwait(false);

            return success;
        }

        public async Task<bool> DeleteScheduledExerciseAsync(int id)
        {
            var result = await _connection.Table<ScheduledExercise>()
                .Where(se => se.Id == id)
                .DeleteAsync()
                .ConfigureAwait(false);
            return result > 0;
        }

        public async Task<int> UpdateMissedExercisesAsync(int userId, DateTime today)
        {
            var todayOnly = today.Date;
            var missed = await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.Status == "PENDING" && se.ScheduledDate < todayOnly)
                .ToListAsync()
                .ConfigureAwait(false);

            if (missed.Count > 0)
            {
                await _connection.RunInTransactionAsync(syncConn =>
                {
                    foreach (var exercise in missed)
                    {
                        exercise.Status = "MISSED";
                        exercise.IsSynced = false;
                        exercise.UpdatedAt = DateTime.UtcNow;
                        syncConn.Update(exercise);
                    }
                }).ConfigureAwait(false);
            }
            return missed.Count;
        }

        public async Task<List<ScheduledExercise>> GetCompletedExercisesAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;
            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.Status == "COMPLETED" && se.ScheduledDate >= start && se.ScheduledDate <= end)
                .OrderBy(se => se.ScheduledDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
