using FitnessApp.Models;

namespace FitnessApp.Services
{
    /// <summary>Read interface for local exercise catalog.</summary>
    public interface IExerciseRepository
    {
        /// <summary>Returns every exercise in the catalog, ordered alphabetically.</summary>
        Task<List<Exercise>> GetAllAsync();

        /// <summary>Filters the catalog by primary muscle (exact match, case-insensitive).</summary>
        Task<List<Exercise>> GetByMuscleAsync(string muscle);

        /// <summary>Substring search over exercise names (case-insensitive).</summary>
        Task<List<Exercise>> SearchByNameAsync(string query);

        /// <summary>Returns a list of all distinct muscles in the catalog.</summary>
        Task<List<string>> GetUniqueMusclesAsync();

        /// <summary>Returns a list of all distinct body parts in the catalog.</summary>
        Task<List<string>> GetUniqueBodyPartsAsync();

        /// <summary>Combined filter pushing search, muscle, and body part criteria directly to SQLite.</summary>
        Task<List<Exercise>> GetFilteredExercisesAsync(string? searchQuery, string? muscle, string? bodyPart, CancellationToken cancellationToken = default);

        /// <summary>Fetches an exercise by its unique string ExerciseId.</summary>
        Task<Exercise?> GetByExerciseIdAsync(string exerciseId);
    }
}
