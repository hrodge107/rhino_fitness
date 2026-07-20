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

        [Column("recurrence_type")]
        public string RecurrenceType { get; set; } = "daily"; // "daily", "weekly", "custom_interval"

        [Column("days_of_week")]
        public string? DaysOfWeek { get; set; } // CSV string e.g. "Mon,Wed,Fri" or "1,3,5"

        [Column("interval_value")]
        public int IntervalValue { get; set; } = 1;

        [Column("interval_unit")]
        public string IntervalUnit { get; set; } = "days"; // "days", "weeks"

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Ignore]
        public string RecurrenceSummaryText
        {
            get
            {
                var timeStr = $"{Hour:D2}:{Minute:D2}";
                if (string.Equals(RecurrenceType, "weekly", StringComparison.OrdinalIgnoreCase))
                {
                    var days = string.IsNullOrWhiteSpace(DaysOfWeek) ? "selected days" : DaysOfWeek;
                    return $"Every {days} at {timeStr}";
                }
                if (string.Equals(RecurrenceType, "custom_interval", StringComparison.OrdinalIgnoreCase))
                {
                    var endStr = EndDate.HasValue ? $" (Ends {EndDate.Value:MMM dd, yyyy HH:mm})" : "";
                    var unitStr = IntervalValue == 1 ? (IntervalUnit == "weeks" ? "week" : "day") : IntervalUnit;
                    return $"Every {IntervalValue} {unitStr} at {timeStr}{endStr}";
                }
                return $"Every day at {timeStr}";
            }
        }
    }
}
