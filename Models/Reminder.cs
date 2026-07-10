using System;
using SQLite;

namespace FitnessApp.Models
{
    [Table("reminders")]
    public class Reminder
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Indexed]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("hour")]
        public int Hour { get; set; }

        [Column("minute")]
        public int Minute { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
