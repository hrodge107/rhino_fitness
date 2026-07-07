using FitnessApp.Models;
using SQLite;
using Postgrest.Models;
using Microsoft.Maui.Networking;

namespace FitnessApp.Services
{
    [Postgrest.Attributes.Table("scheduled_exercises")]
    public class SupabaseScheduledExercise : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public int UserId { get; set; }

        [Postgrest.Attributes.Column("exercise_id")]
        public string ExerciseId { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("scheduled_date")]
        public DateTime ScheduledDate { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string Status { get; set; } = "PENDING";

        [Postgrest.Attributes.Column("is_synced")]
        public bool IsSynced { get; set; } = true;

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Postgrest.Attributes.Column("sets")]
        public int Sets { get; set; }

        [Postgrest.Attributes.Column("duration_seconds")]
        public int DurationSeconds { get; set; }
    }

    public class ScheduledExerciseRepository : IScheduledExerciseRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Supabase.Client _supabaseClient;

        public ScheduledExerciseRepository(IDatabaseService database, Supabase.Client supabaseClient)
        {
            _connection = database.Connection;
            _supabaseClient = supabaseClient;
        }

        private static bool IsOnline()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private static ScheduledExercise ToModel(SupabaseScheduledExercise se)
        {
            return new ScheduledExercise
            {
                Id = se.Id,
                UserId = se.UserId,
                ExerciseId = se.ExerciseId,
                ScheduledDate = se.ScheduledDate.Kind == DateTimeKind.Utc ? se.ScheduledDate.ToLocalTime().Date : se.ScheduledDate.Date,
                Status = se.Status,
                IsSynced = se.IsSynced,
                UpdatedAt = se.UpdatedAt,
                Sets = se.Sets,
                DurationSeconds = se.DurationSeconds
            };
        }

        private static SupabaseScheduledExercise ToSupabase(ScheduledExercise se)
        {
            return new SupabaseScheduledExercise
            {
                Id = se.Id,
                UserId = se.UserId,
                ExerciseId = se.ExerciseId,
                ScheduledDate = se.ScheduledDate,
                Status = se.Status,
                IsSynced = true,
                UpdatedAt = DateTime.UtcNow,
                Sets = se.Sets,
                DurationSeconds = se.DurationSeconds
            };
        }

        public async Task<List<ScheduledExercise>> GetScheduledExercisesForDateAsync(int userId, DateTime date)
        {
            var dateOnly = date.Date;
            var nextDay = dateOnly.AddDays(1);

            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.UserId == userId && x.ScheduledDate >= dateOnly && x.ScheduledDate < nextDay)
                        .Get()
                        .ConfigureAwait(false);

                    var models = result.Models.Select(ToModel).ToList();
                    foreach (var m in models)
                    {
                        await _connection.InsertOrReplaceAsync(m).ConfigureAwait(false);
                    }
                    return models;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] GetScheduledExercisesForDateAsync Supabase Error: {ex.GetType().Name} - {ex.Message}");
                    // Fallback to local on error
                }
            }

            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.ScheduledDate >= dateOnly && se.ScheduledDate < nextDay)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> AddScheduledExercisesAsync(IEnumerable<ScheduledExercise> exercises)
        {
            var success = true;

            foreach (var exercise in exercises)
            {
                exercise.ScheduledDate = exercise.ScheduledDate.Date;
                exercise.IsSynced = false;
                exercise.UpdatedAt = DateTime.UtcNow;

                if (IsOnline())
                {
                    try
                    {
                        var supabaseModel = ToSupabase(exercise);
                        var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                            .Insert(supabaseModel)
                            .ConfigureAwait(false);

                        var created = result.Models.FirstOrDefault();
                        if (created != null)
                        {
                            exercise.Id = created.Id;
                            exercise.IsSynced = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Supabase AddScheduledExercises Error: {ex.Message}");
                        // Save locally as unsynced
                    }
                }

                var rows = await _connection.InsertAsync(exercise).ConfigureAwait(false);
                if (rows <= 0)
                {
                    success = false;
                }
            }

            return success;
        }

        public async Task<bool> DeleteScheduledExerciseAsync(int id)
        {
            if (IsOnline())
            {
                try
                {
                    await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.Id == id)
                        .Delete()
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Delete locally anyway
                }
            }

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
                foreach (var exercise in missed)
                {
                    exercise.Status = "MISSED";
                    exercise.IsSynced = false;
                    exercise.UpdatedAt = DateTime.UtcNow;

                    if (IsOnline())
                    {
                        try
                        {
                            var supabaseModel = ToSupabase(exercise);
                            await _supabaseClient.From<SupabaseScheduledExercise>()
                                .Update(supabaseModel)
                                .ConfigureAwait(false);
                            exercise.IsSynced = true;
                        }
                        catch
                        {
                            // Keep local unsynced flag
                        }
                    }

                    await _connection.UpdateAsync(exercise).ConfigureAwait(false);
                }
            }
            return missed.Count;
        }

        public async Task<List<ScheduledExercise>> GetCompletedExercisesAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.UserId == userId && x.Status == "COMPLETED" && x.ScheduledDate >= start && x.ScheduledDate <= end)
                        .Get()
                        .ConfigureAwait(false);

                    var models = result.Models.Select(ToModel).ToList();
                    foreach (var m in models)
                    {
                        await _connection.InsertOrReplaceAsync(m).ConfigureAwait(false);
                    }
                    return models;
                }
                catch
                {
                    // Fallback to local on error
                }
            }

            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.Status == "COMPLETED" && se.ScheduledDate >= start && se.ScheduledDate <= end)
                .OrderBy(se => se.ScheduledDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<ScheduledExercise?> GetByIdAsync(int id)
        {
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.Id == id)
                        .Get()
                        .ConfigureAwait(false);

                    var model = result.Models.FirstOrDefault();
                    if (model != null)
                    {
                        var mapped = ToModel(model);
                        await _connection.InsertOrReplaceAsync(mapped).ConfigureAwait(false);
                        return mapped;
                    }
                }
                catch
                {
                    // Fallback to local on error
                }
            }

            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.Id == id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> UpdateScheduledExerciseAsync(ScheduledExercise exercise)
        {
            exercise.IsSynced = false;
            exercise.UpdatedAt = DateTime.UtcNow;

            if (IsOnline())
            {
                try
                {
                    var supabaseModel = ToSupabase(exercise);
                    await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Update(supabaseModel)
                        .ConfigureAwait(false);
                    exercise.IsSynced = true;
                }
                catch
                {
                    // Remain unsynced, save locally
                }
            }

            var resultDb = await _connection.UpdateAsync(exercise).ConfigureAwait(false);
            return resultDb > 0;
        }

        public async Task<bool> HasOlderCompletedExercisesAsync(int userId, DateTime startDate)
        {
            // ponytail: simple check logic
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(se => se.UserId == userId && se.Status == "COMPLETED" && se.ScheduledDate < startDate)
                        .Limit(1)
                        .Get()
                        .ConfigureAwait(false);

                    if (result.Models.Any()) return true;
                }
                catch
                {
                    // Fallback to local
                }
            }

            var localOlder = await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.Status == "COMPLETED" && se.ScheduledDate < startDate)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            return localOlder != null;
        }

        public async Task<List<ScheduledExercise>> GetScheduledExercisesForRangeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.UserId == userId && x.ScheduledDate >= start && x.ScheduledDate <= end)
                        .Get()
                        .ConfigureAwait(false);

                    var models = result.Models.Select(ToModel).ToList();
                    foreach (var m in models)
                    {
                        await _connection.InsertOrReplaceAsync(m).ConfigureAwait(false);
                    }
                    return models;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] GetScheduledExercisesForRangeAsync Supabase Error: {ex.GetType().Name} - {ex.Message}");
                    // Fallback to local on error
                }
            }

            return await _connection.Table<ScheduledExercise>()
                .Where(se => se.UserId == userId && se.ScheduledDate >= start && se.ScheduledDate <= end)
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
