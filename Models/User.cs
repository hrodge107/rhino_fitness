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

        [Column("is_synced")]
        public bool IsSynced { get; set; } = false;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
