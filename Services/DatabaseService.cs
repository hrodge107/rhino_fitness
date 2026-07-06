using System.Text.Json;
using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    /// <summary>
    /// Singleton <see cref="IDatabaseService"/> implementation. Opens one
    /// async SQLite connection for the app lifetime and seeds the exercise
    /// catalog transactionally from the packaged JSON asset.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        // Lazy-init: the connection is built on first access so the constructor
        // stays cheap and exception-free for DI.
        private readonly Lazy<SQLiteAsyncConnection> _connection;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DatabaseService()
        {
            _connection = new Lazy<SQLiteAsyncConnection>(() =>
            {
                // bundle_e_sqlite3 provides the native engine; initialize once per process.
                SQLitePCL.Batteries_V2.Init();

                var path = Path.Combine(FileSystem.AppDataDirectory, "fitnessapp.db3");
                var conn = new SQLiteAsyncConnection(
                    path,
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

                // Foreign-key enforcement is off by default in SQLite; flip it on for the
                // normalized workout graph (Workout → WorkoutSet → Exercise) coming later.
                _ = conn.ExecuteAsync("PRAGMA foreign_keys = ON;");
                return conn;
            }, isThreadSafe: true);
        }

        /// <inheritdoc />
        public SQLiteAsyncConnection Connection => _connection.Value;

        /// <inheritdoc />
        public event EventHandler? OnSeedCompleted;

        /// <inheritdoc />
        public bool IsSeedComplete { get; private set; }

        /// <inheritdoc />
        public async Task SeedAsync()
        {
            var db = Connection;
            await db.CreateTableAsync<Exercise>().ConfigureAwait(false);
            await db.CreateTableAsync<User>().ConfigureAwait(false);
            await db.CreateTableAsync<ScheduledExercise>().ConfigureAwait(false);

            // Seed default user Adam if not present
            var existingAdam = await db.Table<User>().Where(u => u.Name == "Adam").FirstOrDefaultAsync().ConfigureAwait(false);
            if (existingAdam == null)
            {
                await db.InsertAsync(new User
                {
                    Name = "Adam",
                    Email = "adam@fitnessapp.com",
                    Password = "test123",
                    IsSynced = true,
                    UpdatedAt = DateTime.UtcNow,
                    Gender = "Male",
                    Age = 25,
                    HeightValue = 69,
                    HeightUnit = "ft/in",
                    WeightValue = 75.0,
                    WeightUnit = "kg"
                }).ConfigureAwait(false);
            }

            // ponytail: clear existing to force re-seed from updated JSON resource
            await db.DeleteAllAsync<Exercise>().ConfigureAwait(false);

            // Open the packaged MauiAsset and deserialize on a background thread.
            // LogicalName resolves to the relative path ("exercises.json") per the csproj ItemGroup.
            await using var stream = await FileSystem.OpenAppPackageFileAsync("exercises.json").ConfigureAwait(false);
            var exercises = await JsonSerializer
                .DeserializeAsync<List<Exercise>>(stream, _jsonOptions)
                .ConfigureAwait(false);

            if (exercises is null || exercises.Count == 0)
            {
                return;
            }

            // Bulk-insert inside a single transaction for atomicity and speed — a partial
            // seed would leave the catalog corrupt.
            await db.RunInTransactionAsync(syncConn =>
            {
                // SQLiteAsyncConnection.RunInTransactionAsync hands us a synchronous connection;
                // use the bulk insert API on it to keep this O(n) in round-trips.
                var total = 0;
                foreach (var batch in Chunk(exercises, 500))
                {
                    total += syncConn.InsertAll(batch);
                }
            }).ConfigureAwait(false);

            IsSeedComplete = true;
            OnSeedCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Lazy chunker — keeps each bulk-insert batch DB-friendly (hundreds, not thousands).</summary>
        private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
            {
                yield return source.GetRange(i, Math.Min(size, source.Count - i));
            }
        }
    }
}
