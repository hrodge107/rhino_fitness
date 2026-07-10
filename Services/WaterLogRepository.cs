using FitnessApp.Models;
using Postgrest.Models;
using Microsoft.Maui.Networking;

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
        private readonly Supabase.Client _supabaseClient;

        public WaterLogRepository(Supabase.Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
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
            try
            {
                var result = await _supabaseClient.From<SupabaseWaterLog>()
                    .Where(x => x.UserId == userId && x.LogDate == date)
                    .Get()
                    .ConfigureAwait(false);

                return result.Models.Select(ToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch water logs from Supabase: {ex.Message}");
                return new List<WaterLog>();
            }
        }

        public async Task<bool> AddWaterLogAsync(WaterLog log)
        {
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
            try
            {
                await _supabaseClient.From<SupabaseWaterLog>().Where(x => x.Id == id).Delete().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete water log from Supabase: {ex.Message}");
                return false;
            }
        }
    }
}
