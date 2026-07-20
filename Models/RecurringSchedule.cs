using System;
using SQLite;

namespace FitnessApp.Models
{
    [Table("recurring_schedules")]
    public class RecurringSchedule
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("user_id"), Indexed, NotNull]
        public int UserId { get; set; }

        [Column("pattern_type"), NotNull]
        public string PatternType { get; set; } = "daily"; // "daily", "weekly", "specific_days", "every_n"

        [Column("days_of_week")]
        public string? DaysOfWeek { get; set; } // CSV string e.g. "1,3,5" for Mon,Wed,Fri

        [Column("interval_value")]
        public int IntervalValue { get; set; } = 1;

        [Column("interval_unit"), NotNull]
        public string IntervalUnit { get; set; } = "days"; // "days", "weeks"

        [Column("exercise_ids"), NotNull]
        public string ExerciseIds { get; set; } = string.Empty; // CSV string e.g. "ex1,ex2"

        [Column("start_date"), NotNull]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("max_occurrences")]
        public int? MaxOccurrences { get; set; }

        [Column("last_generated_date"), NotNull]
        public DateTime LastGeneratedDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
