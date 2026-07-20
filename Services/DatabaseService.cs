using System.Text.Json;
using FitnessApp.Models;
using SQLite;

namespace FitnessApp.Services
{
    /// <summary>Singleton db connection and seed engine.</summary>
    public class DatabaseService : IDatabaseService
    {
        // Init connection lazily to keep CTOR cheap
        private readonly Lazy<SQLiteAsyncConnection> _connection;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DatabaseService()
        {
            _connection = new Lazy<SQLiteAsyncConnection>(() =>
            {
                // Setup native SQLite engine
                SQLitePCL.Batteries_V2.Init();

                var path = Path.Combine(FileSystem.AppDataDirectory, "fitnessapp.db3");
                var conn = new SQLiteAsyncConnection(
                    path,
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache,
                    storeDateTimeAsTicks: false);

                // Enable FK constraints
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

            try { await db.ExecuteAsync("ALTER TABLE users ADD COLUMN water_limit REAL DEFAULT 3000"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN created_at DATETIME DEFAULT CURRENT_TIMESTAMP"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN sets INTEGER DEFAULT 0"); }
            catch { /* column already exists */ }

            try { await db.ExecuteAsync("ALTER TABLE scheduled_exercises ADD COLUMN duration_seconds INTEGER DEFAULT 0"); }
            catch { /* column already exists */ }

            // Migrate user ticks to ISO dates
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

            // Migrate exercise ticks to ISO dates
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

            // Backfill missing sync_ids
            var usersWithoutSyncId = await db.QueryAsync<User>(
                "SELECT * FROM users WHERE sync_id IS NULL OR sync_id = ''");
            foreach (var user in usersWithoutSyncId)
            {
                user.SyncId = Guid.NewGuid().ToString();
                await db.UpdateAsync(user);
            }

            // Ensure water_logs table
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
            await db.CreateTableAsync<Reminder>().ConfigureAwait(false);

            await MigrateDatabaseAsync(db).ConfigureAwait(false);


            // Load packaged exercises catalog JSON
            await using var stream = await FileSystem.OpenAppPackageFileAsync("exercises.json").ConfigureAwait(false);
            var exercises = await JsonSerializer
                .DeserializeAsync<List<Exercise>>(stream, _jsonOptions)
                .ConfigureAwait(false);

            if (exercises is null || exercises.Count == 0)
            {
                return;
            }

            // Transactional bulk insert to avoid partial seeds
            await db.RunInTransactionAsync(syncConn =>
            {
                // Batch inserts on sync connection for speed
                var total = 0;
                foreach (var batch in Chunk(exercises, 500))
                {
                    total += syncConn.InsertAll(batch);
                }
            }).ConfigureAwait(false);

            IsSeedComplete = true;
            OnSeedCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Chunk generator for DB batching.</summary>
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
