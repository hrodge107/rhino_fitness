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
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache,
                    storeDateTimeAsTicks: false);

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

        public static async Task MigrateDatabaseAsync(SQLiteAsyncConnection db)
        {
            try { await db.ExecuteAsync("ALTER TABLE users ADD COLUMN sync_id TEXT"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE users ADD COLUMN created_at DATETIME DEFAULT CURRENT_TIMESTAMP"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE users ADD COLUMN is_active INTEGER DEFAULT 0"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE users ADD COLUMN calorie_limit REAL DEFAULT 2000"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN created_at DATETIME DEFAULT CURRENT_TIMESTAMP"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN sets INTEGER DEFAULT 0"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN duration_seconds INTEGER DEFAULT 0"); }
            catch { /* column already exists */ }

            // Convert ticks stored as dates in users
            try
            {
                var usersRaw = await db.QueryAsync<RawUserDates>("SELECT id, created_at as CreatedAt, updated_at as UpdatedAt FROM users");
                foreach (var u in usersRaw)
                {
                    bool updated = false;
                    string? newCreated = u.CreatedAt;
                    string? newUpdated = u.UpdatedAt;

                    if (long.TryParse(u.CreatedAt, out long cTicks) && cTicks > 0)
                    {
                        newCreated = new DateTime(cTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        updated = true;
                    }
                    if (long.TryParse(u.UpdatedAt, out long uTicks) && uTicks > 0)
                    {
                        newUpdated = new DateTime(uTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        updated = true;
                    }

                    if (updated)
                    {
                        await db.ExecuteAsync("UPDATE users SET created_at = ?, updated_at = ? WHERE id = ?", newCreated, newUpdated, u.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed user DateTime migration: {ex.Message}");
            }

            // Convert ticks stored as dates in scheduled_exercises
            try
            {
                var schedRaw = await db.QueryAsync<RawScheduledDates>("SELECT id, scheduled_date as ScheduledDate, updated_at as UpdatedAt, created_at as CreatedAt FROM scheduled_exercises");
                foreach (var s in schedRaw)
                {
                    bool updated = false;
                    string? newSched = s.ScheduledDate;
                    string? newUpdated = s.UpdatedAt;
                    string? newCreated = s.CreatedAt;

                    if (long.TryParse(s.ScheduledDate, out long sTicks) && sTicks > 0)
                    {
                        newSched = new DateTime(sTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        updated = true;
                    }
                    if (long.TryParse(s.UpdatedAt, out long uTicks) && uTicks > 0)
                    {
                        newUpdated = new DateTime(uTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        updated = true;
                    }
                    if (long.TryParse(s.CreatedAt, out long cTicks) && cTicks > 0)
                    {
                        newCreated = new DateTime(cTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        updated = true;
                    }

                    if (updated)
                    {
                        await db.ExecuteAsync("UPDATE scheduled_exercises SET scheduled_date = ?, updated_at = ?, created_at = ? WHERE id = ?", newSched, newUpdated, newCreated, s.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed scheduled_exercises DateTime migration: {ex.Message}");
            }

            // Backfill sync_id for existing rows that have NULL
            var usersWithoutSyncId = await db.QueryAsync<User>(
                "SELECT * FROM users WHERE sync_id IS NULL OR sync_id = ''");
            foreach (var user in usersWithoutSyncId)
            {
                user.SyncId = Guid.NewGuid().ToString();
                await db.UpdateAsync(user);
            }

            // Create water_logs table if not exists during migration
            await db.CreateTableAsync<WaterLog>().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SeedAsync()
        {
            var db = Connection;
            await db.ExecuteAsync("DROP TABLE IF EXISTS exercises").ConfigureAwait(false);
            await db.ExecuteAsync("DROP TABLE IF EXISTS meal_logs").ConfigureAwait(false);
            await db.ExecuteAsync("DROP TABLE IF EXISTS water_logs").ConfigureAwait(false);
            await db.CreateTableAsync<Exercise>().ConfigureAwait(false);
            await db.CreateTableAsync<User>().ConfigureAwait(false);
            await db.CreateTableAsync<ScheduledExercise>().ConfigureAwait(false);
            await db.CreateTableAsync<MealLog>().ConfigureAwait(false);
            await db.CreateTableAsync<WaterLog>().ConfigureAwait(false);

            await MigrateDatabaseAsync(db).ConfigureAwait(false);


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

        private class RawUserDates
        {
            public int Id { get; set; }
            public string? CreatedAt { get; set; }
            public string? UpdatedAt { get; set; }
        }

        private class RawScheduledDates
        {
            public int Id { get; set; }
            public string? ScheduledDate { get; set; }
            public string? UpdatedAt { get; set; }
            public string? CreatedAt { get; set; }
        }
    }
}
