using FitnessApp.Models;
using SQLite;
using Postgrest.Models;
using Microsoft.Maui.Networking;

namespace FitnessApp.Services
{
    [Postgrest.Attributes.Table("profiles")]
    public class SupabaseProfile : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int? Id { get; set; }

        [Postgrest.Attributes.Column("email")]
        public string Email { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("name")]
        public string Name { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("password")]
        public string Password { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("gender")]
        public string Gender { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("age")]
        public int Age { get; set; }

        [Postgrest.Attributes.Column("height_value")]
        public double HeightValue { get; set; }

        [Postgrest.Attributes.Column("height_unit")]
        public string HeightUnit { get; set; } = "ft/in";

        [Postgrest.Attributes.Column("weight_value")]
        public double WeightValue { get; set; }

        [Postgrest.Attributes.Column("weight_unit")]
        public string WeightUnit { get; set; } = "kg";

        [Postgrest.Attributes.Column("is_synced")]
        public bool IsSynced { get; set; } = true;

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Postgrest.Attributes.Column("sync_id")]
        public string SyncId { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Postgrest.Attributes.Column("calorie_limit")]
        public double CalorieLimit { get; set; } = 2000;

        [Postgrest.Attributes.Column("water_limit")]
        public double WaterLimit { get; set; } = 3000;
    }

    public class UserRepository : IUserRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Supabase.Client _supabaseClient;
        private readonly SessionService _sessionService;

        public UserRepository(IDatabaseService database, Supabase.Client supabaseClient, SessionService sessionService)
        {
            _connection = database.Connection;
            _supabaseClient = supabaseClient;
            _sessionService = sessionService;
        }

        private static bool IsOnline()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private static User ToUser(SupabaseProfile profile)
        {
            return new User
            {
                Id = profile.Id ?? 0,
                Email = profile.Email,
                Name = profile.Name,
                Password = profile.Password,
                Gender = profile.Gender,
                Age = profile.Age,
                HeightValue = profile.HeightValue,
                HeightUnit = profile.HeightUnit,
                WeightValue = profile.WeightValue,
                WeightUnit = profile.WeightUnit,
                IsSynced = profile.IsSynced,
                UpdatedAt = profile.UpdatedAt,
                SyncId = profile.SyncId,
                CreatedAt = profile.CreatedAt,
                CalorieLimit = profile.CalorieLimit,
                WaterLimit = profile.WaterLimit
            };
        }

        private static SupabaseProfile ToProfile(User user)
        {
            return new SupabaseProfile
            {
                Email = user.Email,
                Name = user.Name,
                Password = user.Password,
                Gender = user.Gender,
                Age = user.Age,
                HeightValue = user.HeightValue,
                HeightUnit = user.HeightUnit,
                WeightValue = user.WeightValue,
                WeightUnit = user.WeightUnit,
                IsSynced = user.IsSynced,
                UpdatedAt = user.UpdatedAt,
                SyncId = user.SyncId,
                CreatedAt = user.CreatedAt,
                CalorieLimit = user.CalorieLimit,
                WaterLimit = user.WaterLimit
            };
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseProfile>()
                        .Where(x => x.Id == id)
                        .Get()
                        .ConfigureAwait(false);

                    var profile = result.Models.FirstOrDefault();
                    if (profile != null)
                    {
                        var user = ToUser(profile);
                        await _connection.InsertOrReplaceAsync(user).ConfigureAwait(false);
                        return user;
                    }
                }
                catch
                {
                    // Fallback to local on error
                }
            }

            return await _connection.Table<User>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await GetOrFetchUserAsync(email).ConfigureAwait(false);
        }

        public async Task<User?> GetByNameAsync(string name)
        {
            var target = name.Trim().ToLowerInvariant();

            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseProfile>()
                        .Where(x => x.Name == target)
                        .Get()
                        .ConfigureAwait(false);

                    var profile = result.Models.FirstOrDefault();
                    if (profile != null)
                    {
                        var user = ToUser(profile);
                        await _connection.InsertOrReplaceAsync(user).ConfigureAwait(false);
                        return user;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Supabase GetByName Error: {ex.Message}");
                    // Fallback to local on error
                }
            }

            return await _connection.Table<User>()
                .Where(u => u.Name == target)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<User?> ValidateUserAsync(string usernameOrEmail, string password)
        {
            var target = usernameOrEmail.Trim().ToLowerInvariant();
            if (target.Contains("@"))
            {
                return await LoginAsync(target, password).ConfigureAwait(false);
            }

            var localUser = await GetByNameAsync(target).ConfigureAwait(false);
            if (localUser != null && BCrypt.Net.BCrypt.Verify(password, localUser.Password))
            {
                await _sessionService.SetActiveUserAsync(localUser).ConfigureAwait(false);
                return localUser;
            }

            return null;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            return await SaveUserAsync(user).ConfigureAwait(false);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            return await SaveUserAsync(user).ConfigureAwait(false);
        }

        public async Task<User?> GetOrFetchUserAsync(string email)
        {
            var target = email.Trim().ToLowerInvariant();

            var localUser = await _connection.Table<User>()
                .Where(u => u.Email == target)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (!IsOnline())
            {
                return localUser;
            }

            try
            {
                var result = await _supabaseClient.From<SupabaseProfile>()
                    .Where(x => x.Email == target)
                    .Get()
                    .ConfigureAwait(false);

                var cloudProfile = result.Models.FirstOrDefault();

                if (cloudProfile == null && localUser == null)
                {
                    return null;
                }

                if (cloudProfile == null && localUser != null)
                {
                    return localUser;
                }

                if (cloudProfile != null)
                {
                    var localBySyncId = await _connection.Table<User>()
                        .Where(u => u.SyncId == cloudProfile.SyncId)
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                    if (localBySyncId != null)
                    {
                        if (cloudProfile.UpdatedAt > localBySyncId.UpdatedAt)
                        {
                            var updatedUser = ToUser(cloudProfile);
                            updatedUser.Id = localBySyncId.Id;
                            updatedUser.IsSynced = true;
                            await _connection.UpdateAsync(updatedUser).ConfigureAwait(false);
                            localUser = updatedUser;
                        }
                        else if (localBySyncId.UpdatedAt > cloudProfile.UpdatedAt)
                        {
                            var profileToPush = ToProfile(localBySyncId);
                            await _supabaseClient.From<SupabaseProfile>()
                                .OnConflict("sync_id")
                                .Upsert(profileToPush)
                                .ConfigureAwait(false);

                            localBySyncId.IsSynced = true;
                            await _connection.UpdateAsync(localBySyncId).ConfigureAwait(false);
                            localUser = localBySyncId;
                        }
                        else
                        {
                            if (!localBySyncId.IsSynced)
                            {
                                localBySyncId.IsSynced = true;
                                await _connection.UpdateAsync(localBySyncId).ConfigureAwait(false);
                            }
                            localUser = localBySyncId;
                        }
                    }
                    else
                    {
                        var newUser = ToUser(cloudProfile);
                        newUser.IsSynced = true;
                        if (localUser != null)
                        {
                            newUser.Id = localUser.Id;
                            await _connection.UpdateAsync(newUser).ConfigureAwait(false);
                        }
                        else
                        {
                            await _connection.InsertAsync(newUser).ConfigureAwait(false);
                        }
                        localUser = newUser;
                    }

                    await _sessionService.SetActiveUserAsync(localUser).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Supabase GetOrFetchUser Error: {ex.Message}");
                // Fallback to local on error
            }

            return localUser;
        }

        public async Task<bool> SaveUserAsync(User user)
        {
            if (!IsOnline()) return false;

            user.UpdatedAt = DateTime.UtcNow;

            if (user.Id == 0)
            {
                user.SyncId = Guid.NewGuid().ToString();
                user.CreatedAt = DateTime.UtcNow;
            }

            try
            {
                var profile = ToProfile(user);
                if (user.Id != 0)
                    profile.Id = user.Id;
                var response = user.Id == 0
                    ? await _supabaseClient.From<SupabaseProfile>()
                        .Insert(profile)
                        .ConfigureAwait(false)
                    : await _supabaseClient.From<SupabaseProfile>()
                        .OnConflict("sync_id")
                        .Upsert(profile)
                        .ConfigureAwait(false);

                if (response.Models.Any())
                {
                    var cloudProfile = response.Models.First();
                    if (user.Id == 0)
                    {
                        user.Id = cloudProfile.Id ?? 0;
                    }
                    user.IsSynced = true;

                    var existing = await _connection.Table<User>()
                        .Where(u => u.SyncId == user.SyncId)
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                    if (existing != null)
                    {
                        user.Id = existing.Id;
                        await _connection.UpdateAsync(user).ConfigureAwait(false);
                    }
                    else
                    {
                        await _connection.InsertAsync(user).ConfigureAwait(false);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Supabase SaveUser Error: {ex.Message}");
            }

            return false;
        }

        public async Task SyncPendingChangesAsync()
        {
            var unsyncedUsers = await _connection.Table<User>()
                .Where(u => !u.IsSynced)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var user in unsyncedUsers)
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseProfile>()
                        .Where(x => x.SyncId == user.SyncId)
                        .Get()
                        .ConfigureAwait(false);

                    var cloudProfile = result.Models.FirstOrDefault();

                    if (cloudProfile == null)
                    {
                        var profile = ToProfile(user);
                        await _supabaseClient.From<SupabaseProfile>()
                            .Insert(profile)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        if (user.UpdatedAt >= cloudProfile.UpdatedAt)
                        {
                            var profile = ToProfile(user);
                            await _supabaseClient.From<SupabaseProfile>()
                                .OnConflict("sync_id")
                                .Upsert(profile)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            var cloudUser = ToUser(cloudProfile);
                            cloudUser.Id = user.Id;
                            cloudUser.IsSynced = true;
                            await _connection.UpdateAsync(cloudUser).ConfigureAwait(false);
                            continue;
                        }
                    }

                    user.IsSynced = true;
                    await _connection.UpdateAsync(user).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error syncing user {user.Email}: {ex.Message}");
                }
            }
        }

        public async Task RefreshActiveUserAsync()
        {
            var activeUser = await _sessionService.GetActiveUserAsync().ConfigureAwait(false);
            if (activeUser == null)
            {
                return;
            }

            try
            {
                var result = await _supabaseClient.From<SupabaseProfile>()
                    .Where(x => x.SyncId == activeUser.SyncId)
                    .Get()
                    .ConfigureAwait(false);

                var cloudProfile = result.Models.FirstOrDefault();

                if (cloudProfile != null && cloudProfile.UpdatedAt > activeUser.UpdatedAt)
                {
                    var updatedUser = ToUser(cloudProfile);
                    updatedUser.Id = activeUser.Id;
                    updatedUser.IsSynced = true;
                    await _connection.UpdateAsync(updatedUser).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing active user: {ex.Message}");
            }
        }

        public async Task<User?> LoginAsync(string email, string rawPassword)
        {
            var user = await GetOrFetchUserAsync(email).ConfigureAwait(false);
            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(rawPassword, user.Password))
                return null;

            await _sessionService.SetActiveUserAsync(user).ConfigureAwait(false);
            return user;
        }
    }
}
