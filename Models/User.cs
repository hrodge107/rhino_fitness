using SQLite;

namespace FitnessApp.Models
{
    [Table("users")]
    public class User
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("email")]
        [Unique, NotNull]
        public string Email { get; set; } = string.Empty;

        [Column("name")]
        [NotNull]
        public string Name { get; set; } = string.Empty;

        [Column("password")]
        [NotNull]
        public string Password { get; set; } = string.Empty;

        [Column("sync_id")]
        public string SyncId { get; set; } = Guid.NewGuid().ToString();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = false;

        [Column("is_synced")]
        public bool IsSynced { get; set; } = false;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("gender")]
        public string Gender { get; set; } = string.Empty;

        [Column("age")]
        public int Age { get; set; }

        [Column("height_value")]
        public double HeightValue { get; set; }

        [Column("height_unit")]
        public string HeightUnit { get; set; } = "ft/in";

        [Column("weight_value")]
        public double WeightValue { get; set; }

        [Column("weight_unit")]
        public string WeightUnit { get; set; } = "kg";

        [Column("calorie_limit")]
        public double CalorieLimit { get; set; } = 2000;
    }
}
