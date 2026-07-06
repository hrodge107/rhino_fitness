using SQLite;

namespace FitnessApp.Models
{
    [Table("scheduled_exercises")]
    public class ScheduledExercise
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Indexed, NotNull]
        public int UserId { get; set; }

        [Column("exercise_id")]
        [Indexed, NotNull]
        public string ExerciseId { get; set; } = string.Empty;

        [Column("scheduled_date")]
        [Indexed, NotNull]
        public DateTime ScheduledDate { get; set; }

        [Column("status")]
        [NotNull]
        public string Status { get; set; } = "PENDING"; // PENDING, COMPLETED, MISSED

        [Column("is_synced")]
        public bool IsSynced { get; set; } = false;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
