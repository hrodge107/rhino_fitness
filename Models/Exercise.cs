using SQLite;
using System.Text.Json.Serialization;

namespace FitnessApp.Models
{
    [Table("exercises")]
    public class Exercise
    {
        /// <summary>Auto-increment primary key; stable identity for updates and FK references.</summary>
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        [Column("name")]
        [NotNull]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("exerciseId")]
        [Column("exercise_id")]
        [Indexed(Unique = true)]
        public string ExerciseId { get; set; } = string.Empty;

        [JsonPropertyName("gifUrl")]
        [Column("gif_url")]
        public string GifUrl { get; set; } = string.Empty;

        [JsonPropertyName("bodyPart")]
        [Column("body_part")]
        public string BodyPart { get; set; } = string.Empty;

        [JsonPropertyName("muscle")]
        [Column("muscle")]
        public string Muscle { get; set; } = string.Empty;

        /// <summary>Nullable in the source feed (bodyweight drills carry no equipment).</summary>
        [JsonPropertyName("equipment")]
        [Column("equipment")]
        public string? Equipment { get; set; }

        [JsonPropertyName("instructions")]
        [Column("instructions")]
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Sync flag for the deferred Phase-2 Supabase layer. Seed rows are
        /// treated as already reconciled with the origin feed, so default true.
        /// </summary>
        [Column("is_synced")]
        public bool IsSynced { get; set; } = true;

        /// <summary>UTC timestamp of the last local mutation; drives diffing in Phase-2 sync.</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
