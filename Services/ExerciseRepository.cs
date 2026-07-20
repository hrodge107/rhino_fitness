using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    /// <summary>SQLite implementation of exercise repository pushing queries to DB.</summary>
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        private class MuscleResult
        {
            public string Muscle { get; set; } = string.Empty;
        }

        private class BodyPartResult
        {
            public string BodyPart { get; set; } = string.Empty;
        }

        public ExerciseRepository(IDatabaseService database)
        {
            // DIP: depend on DB abstraction
            _connection = database.Connection;
        }

        /// <inheritdoc />
        public async Task<List<Exercise>> GetAllAsync()
        {
            return await _connection.Table<Exercise>()
                .OrderBy(e => e.Name)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Exercise>> GetByMuscleAsync(string muscle)
        {
            var target = muscle.Trim().ToLowerInvariant();
            return await _connection.Table<Exercise>()
                .Where(e => e.Muscle.ToLower() == target)
                .OrderBy(e => e.Name)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Exercise>> SearchByNameAsync(string query)
        {
            var term = query.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(term))
            {
                return await GetAllAsync().ConfigureAwait(false);
            }

            // Case-insensitive match pushed to SQLite
            return await _connection.Table<Exercise>()
                .Where(e => e.Name.ToLower().Contains(term))
                .OrderBy(e => e.Name)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUniqueMusclesAsync()
        {
            var results = await _connection.QueryAsync<MuscleResult>(
                "SELECT DISTINCT muscle AS Muscle FROM exercises WHERE muscle IS NOT NULL AND muscle != '' ORDER BY muscle")
                .ConfigureAwait(false);
            return results.Select(r => r.Muscle).ToList();
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUniqueBodyPartsAsync()
        {
            var results = await _connection.QueryAsync<BodyPartResult>(
                "SELECT DISTINCT body_part AS BodyPart FROM exercises WHERE body_part IS NOT NULL AND body_part != '' ORDER BY body_part")
                .ConfigureAwait(false);
            return results.Select(r => r.BodyPart).ToList();
        }

        /// <inheritdoc />
        public async Task<List<Exercise>> GetFilteredExercisesAsync(string? searchQuery, string? muscle, string? bodyPart, CancellationToken cancellationToken = default)
        {
            var query = _connection.Table<Exercise>();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(e => e.Name.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(muscle) && !string.Equals(muscle, "All Muscles", StringComparison.OrdinalIgnoreCase))
            {
                var muscleTarget = muscle.Trim().ToLowerInvariant();
                query = query.Where(e => e.Muscle.ToLower() == muscleTarget);
            }

            if (!string.IsNullOrWhiteSpace(bodyPart) && !string.Equals(bodyPart, "All Body Parts", StringComparison.OrdinalIgnoreCase))
            {
                var bodyPartTarget = bodyPart.Trim().ToLowerInvariant();
                query = query.Where(e => e.BodyPart.ToLower() == bodyPartTarget);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var results = await query
                .OrderBy(e => e.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return results;
        }

        /// <inheritdoc />
        public async Task<Exercise?> GetByExerciseIdAsync(string exerciseId)
        {
            return await _connection.Table<Exercise>()
                .Where(e => e.ExerciseId == exerciseId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
    }
}
