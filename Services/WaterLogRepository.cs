using FitnessApp.Models;
using Postgrest.Models;
using Microsoft.Maui.Networking;
using SQLite;

namespace FitnessApp.Services
{
    [Postgrest.Attributes.Table("water_logs")]
    public class SupabaseWaterLog : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public int UserId { get; set; }

        [Postgrest.Attributes.Column("amount")]
        public double Amount { get; set; }

        [Postgrest.Attributes.Column("log_date")]
        public string LogDate { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("is_synced")]
        public bool IsSynced { get; set; } = true;

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WaterLogRepository : IWaterLogRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Supabase.Client _supabaseClient;

        public WaterLogRepository(IDatabaseService database, Supabase.Client supabaseClient)
        {
            _connection = database.Connection;
            _supabaseClient = supabaseClient;
        }

        private static bool IsOnline()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private static WaterLog ToModel(SupabaseWaterLog sLog)
        {
            return new WaterLog
            {
                Id = sLog.Id,
                UserId = sLog.UserId,
                Amount = sLog.Amount,
                LogDate = sLog.LogDate,
                IsSynced = sLog.IsSynced,
                UpdatedAt = sLog.UpdatedAt,
                CreatedAt = sLog.CreatedAt
            };
        }

        private static SupabaseWaterLog ToSupabase(WaterLog log)
        {
            return new SupabaseWaterLog
            {
                Id = log.Id,
                UserId = log.UserId,
                Amount = log.Amount,
                LogDate = log.LogDate,
                IsSynced = log.IsSynced,
                UpdatedAt = log.UpdatedAt,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<List<WaterLog>> GetWaterLogsForDateAsync(int userId, string date)
        {
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseWaterLog>()
                        .Where(x => x.UserId == userId && x.LogDate == date)
                        .Get()
                        .ConfigureAwait(false);

                    var models = result.Models.Select(ToModel).ToList();
                    await _connection.Table<WaterLog>()
                        .Where(x => x.UserId == userId && x.LogDate == date)
                        .DeleteAsync()
                        .ConfigureAwait(false);

                    if (models.Any())
                    {
                        foreach (var m in models)
                            await _connection.InsertOrReplaceAsync(m).ConfigureAwait(false);
                    }
                    return models;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to fetch water logs from Supabase: {ex.Message}");
                }
            }

            return await _connection.Table<WaterLog>()
                .Where(x => x.UserId == userId && x.LogDate == date)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> AddWaterLogAsync(WaterLog log)
        {
            if (!IsOnline()) return false;

            log.CreatedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;
            log.IsSynced = true;

            try
            {
                var sLog = ToSupabase(log);
                var response = await _supabaseClient.From<SupabaseWaterLog>().Insert(sLog).ConfigureAwait(false);
                var created = response.Models.FirstOrDefault();
                if (created != null)
                {
                    log.Id = created.Id;
                    await _connection.InsertOrReplaceAsync(log).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add water log to Supabase: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> DeleteWaterLogAsync(int id)
        {
            if (!IsOnline()) return false;

            try
            {
                await _supabaseClient.From<SupabaseWaterLog>().Where(x => x.Id == id).Delete().ConfigureAwait(false);
                await _connection.Table<WaterLog>().Where(x => x.Id == id).DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete water log from Supabase: {ex.Message}");
            }
            return false;
        }
    }
}
