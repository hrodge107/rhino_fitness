using FitnessApp.Models;
using SQLite;
using Postgrest.Models;
using Microsoft.Maui.Networking;

namespace FitnessApp.Services
{
    [Postgrest.Attributes.Table("meal_logs")]
    public class SupabaseMealLog : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public int UserId { get; set; }

        [Postgrest.Attributes.Column("category")]
        public string Category { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("food_name")]
        public string FoodName { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("calories")]
        public double Calories { get; set; }

        [Postgrest.Attributes.Column("log_date")]
        public string LogDate { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("is_synced")]
        public bool IsSynced { get; set; } = true;

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MealLogRepository : IMealLogRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Supabase.Client _supabaseClient;
        private static readonly SemaphoreSlim _syncSemaphore = new(1, 1);

        public MealLogRepository(IDatabaseService database, Supabase.Client supabaseClient)
        {
            _connection = database.Connection;
            _supabaseClient = supabaseClient;
        }

        private static bool IsOnline()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private static MealLog ToModel(SupabaseMealLog sLog)
        {
            return new MealLog
            {
                Id = sLog.Id,
                UserId = sLog.UserId,
                Category = sLog.Category,
                FoodName = sLog.FoodName,
                Calories = sLog.Calories,
                LogDate = sLog.LogDate,
                IsSynced = sLog.IsSynced,
                UpdatedAt = sLog.UpdatedAt,
                CreatedAt = sLog.CreatedAt
            };
        }

        private static SupabaseMealLog ToSupabase(MealLog log)
        {
            return new SupabaseMealLog
            {
                Id = log.Id,
                UserId = log.UserId,
                Category = log.Category,
                FoodName = log.FoodName,
                Calories = log.Calories,
                LogDate = log.LogDate,
                IsSynced = log.IsSynced,
                UpdatedAt = log.UpdatedAt,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<List<MealLog>> GetMealLogsForDateAsync(int userId, string date)
        {
            try
            {
                var result = await _supabaseClient.From<SupabaseMealLog>()
                    .Where(x => x.UserId == userId && x.LogDate == date)
                    .Get()
                    .ConfigureAwait(false);

                return result.Models.Select(ToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch meal logs from Supabase: {ex.Message}");
                return new List<MealLog>();
            }
        }

        public async Task<bool> AddMealLogAsync(MealLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;
            log.IsSynced = true;

            try
            {
                var sLog = ToSupabase(log);
                var response = await _supabaseClient.From<SupabaseMealLog>().Insert(sLog).ConfigureAwait(false);
                var created = response.Models.FirstOrDefault();
                if (created != null)
                {
                    log.Id = created.Id;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add meal log to Supabase: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> UpdateMealLogAsync(MealLog log)
        {
            log.UpdatedAt = DateTime.UtcNow;

            try
            {
                var sLog = ToSupabase(log);
                var response = await _supabaseClient.From<SupabaseMealLog>().Update(sLog).ConfigureAwait(false);
                return response.Models.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update meal log on Supabase: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMealLogAsync(int id)
        {
            try
            {
                await _supabaseClient.From<SupabaseMealLog>().Where(x => x.Id == id).Delete().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete meal log from Supabase: {ex.Message}");
                return false;
            }
        }

        public async Task<double> GetDailyCaloriesAsync(int userId, string date)
        {
            var logs = await GetMealLogsForDateAsync(userId, date);
            return logs.Sum(x => x.Calories);
        }

        public async Task<double> GetCategoryCaloriesAsync(int userId, string date, string category)
        {
            var logs = await GetMealLogsForDateAsync(userId, date);
            return logs.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Calories);
        }

        public Task SyncPendingLogsAsync(int userId)
        {
            return Task.CompletedTask;
        }
    }
}
