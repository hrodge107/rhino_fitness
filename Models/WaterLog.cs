using SQLite;

namespace FitnessApp.Models
{
    [Table("water_logs")]
    public class WaterLog
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Indexed, NotNull]
        public int UserId { get; set; }

        [Column("amount")]
        [NotNull]
        public double Amount { get; set; } // stored in mL

        [Column("log_date")]
        [Indexed, NotNull]
        public string LogDate { get; set; } = string.Empty; // YYYY-MM-DD format

        [Column("is_synced")]
        public bool IsSynced { get; set; } = false;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
