using SQLite;

namespace FitnessApp.Models
{
    [Table("meal_logs")]
    public class MealLog
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        [Indexed, NotNull]
        public int UserId { get; set; }

        [Column("category")]
        [NotNull]
        public string Category { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, Snack

        [Column("food_name")]
        [NotNull]
        public string FoodName { get; set; } = string.Empty;

        [Column("calories")]
        [NotNull]
        public double Calories { get; set; }

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
