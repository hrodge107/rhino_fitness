using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitnessApp.Models;
using Microsoft.Maui.Networking;
using Postgrest.Models;
using SQLite;

namespace FitnessApp.Services
{
    [Postgrest.Attributes.Table("recurring_schedules")]
    public class SupabaseRecurringSchedule : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int Id { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public int UserId { get; set; }

        [Postgrest.Attributes.Column("pattern_type")]
        public string PatternType { get; set; } = "daily";

        [Postgrest.Attributes.Column("days_of_week")]
        public string? DaysOfWeek { get; set; }

        [Postgrest.Attributes.Column("interval_value")]
        public int IntervalValue { get; set; } = 1;

        [Postgrest.Attributes.Column("interval_unit")]
        public string IntervalUnit { get; set; } = "days";

        [Postgrest.Attributes.Column("exercise_ids")]
        public string ExerciseIds { get; set; } = string.Empty;

        [Postgrest.Attributes.Column("start_date")]
        public DateTime StartDate { get; set; }

        [Postgrest.Attributes.Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Postgrest.Attributes.Column("max_occurrences")]
        public int? MaxOccurrences { get; set; }

        [Postgrest.Attributes.Column("last_generated_date")]
        public DateTime LastGeneratedDate { get; set; }

        [Postgrest.Attributes.Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RecurringScheduleRepository : IRecurringScheduleRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Supabase.Client _supabaseClient;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;

        public RecurringScheduleRepository(
            IDatabaseService database,
            Supabase.Client supabaseClient,
            IScheduledExerciseRepository scheduledExerciseRepository)
        {
            _connection = database.Connection;
            _supabaseClient = supabaseClient;
            _scheduledExerciseRepository = scheduledExerciseRepository;
        }

        private static bool IsOnline()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private static RecurringSchedule ToModel(SupabaseRecurringSchedule s)
        {
            return new RecurringSchedule
            {
                Id = s.Id,
                UserId = s.UserId,
                PatternType = s.PatternType,
                DaysOfWeek = s.DaysOfWeek,
                IntervalValue = s.IntervalValue,
                IntervalUnit = s.IntervalUnit,
                ExerciseIds = s.ExerciseIds,
                StartDate = s.StartDate.Kind == DateTimeKind.Utc ? s.StartDate.ToLocalTime().Date : s.StartDate.Date,
                EndDate = s.EndDate.HasValue ? (s.EndDate.Value.Kind == DateTimeKind.Utc ? s.EndDate.Value.ToLocalTime().Date : s.EndDate.Value.Date) : null,
                MaxOccurrences = s.MaxOccurrences,
                LastGeneratedDate = s.LastGeneratedDate.Kind == DateTimeKind.Utc ? s.LastGeneratedDate.ToLocalTime().Date : s.LastGeneratedDate.Date,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };
        }

        private static SupabaseRecurringSchedule ToSupabase(RecurringSchedule s)
        {
            return new SupabaseRecurringSchedule
            {
                Id = s.Id,
                UserId = s.UserId,
                PatternType = s.PatternType,
                DaysOfWeek = s.DaysOfWeek,
                IntervalValue = s.IntervalValue,
                IntervalUnit = s.IntervalUnit,
                ExerciseIds = s.ExerciseIds,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                MaxOccurrences = s.MaxOccurrences,
                LastGeneratedDate = s.LastGeneratedDate,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };
        }

        public async Task<bool> CreateScheduleAsync(RecurringSchedule schedule)
        {
            schedule.StartDate = schedule.StartDate.Date;
            if (schedule.EndDate.HasValue) schedule.EndDate = schedule.EndDate.Value.Date;
            schedule.LastGeneratedDate = schedule.StartDate.AddDays(-1);
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.IsActive = true;

            if (IsOnline())
            {
                try
                {
                    var supabaseModel = ToSupabase(schedule);
                    var result = await _supabaseClient.From<SupabaseRecurringSchedule>()
                        .Insert(supabaseModel)
                        .ConfigureAwait(false);

                    var created = result.Models.FirstOrDefault();
                    if (created != null)
                    {
                        schedule.Id = created.Id;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Supabase CreateSchedule Error: {ex.Message}");
                }
            }

            await _connection.InsertOrReplaceAsync(schedule).ConfigureAwait(false);

            // Materialize initial 30 days
            await GenerateOccurrencesAsync(schedule, DateTime.Today.AddDays(30)).ConfigureAwait(false);

            return true;
        }

        public async Task<List<RecurringSchedule>> GetActiveSchedulesAsync(int userId)
        {
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseRecurringSchedule>()
                        .Where(x => x.UserId == userId && x.IsActive == true)
                        .Get()
                        .ConfigureAwait(false);

                    var models = result.Models.Select(ToModel).ToList();
                    await _connection.Table<RecurringSchedule>()
                        .Where(x => x.UserId == userId && x.IsActive == true)
                        .DeleteAsync()
                        .ConfigureAwait(false);

                    foreach (var m in models)
                    {
                        await _connection.InsertOrReplaceAsync(m).ConfigureAwait(false);
                    }
                    return models;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetActiveSchedulesAsync Supabase Error: {ex.Message}");
                }
            }

            return await _connection.Table<RecurringSchedule>()
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<RecurringSchedule?> GetByIdAsync(int id)
        {
            if (IsOnline())
            {
                try
                {
                    var result = await _supabaseClient.From<SupabaseRecurringSchedule>()
                        .Where(x => x.Id == id)
                        .Get()
                        .ConfigureAwait(false);

                    var model = result.Models.FirstOrDefault();
                    if (model != null)
                    {
                        var mapped = ToModel(model);
                        await _connection.InsertOrReplaceAsync(mapped).ConfigureAwait(false);
                        return mapped;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetByIdAsync Supabase Error: {ex.Message}");
                }
            }

            return await _connection.Table<RecurringSchedule>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> CancelScheduleAsync(int scheduleId)
        {
            var schedule = await GetByIdAsync(scheduleId);
            if (schedule == null) return false;

            var today = DateTime.Today;
            schedule.EndDate = today;
            schedule.IsActive = false;

            if (IsOnline())
            {
                try
                {
                    var supabaseModel = ToSupabase(schedule);
                    await _supabaseClient.From<SupabaseRecurringSchedule>()
                        .Update(supabaseModel)
                        .ConfigureAwait(false);

                    // Delete future instances from Supabase
                    await _supabaseClient.From<SupabaseScheduledExercise>()
                        .Where(x => x.UserId == schedule.UserId && x.ScheduledDate >= today && x.Status == "PENDING")
                        .Delete()
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CancelScheduleAsync Supabase Error: {ex.Message}");
                }
            }

            await _connection.InsertOrReplaceAsync(schedule).ConfigureAwait(false);

            // Delete future pending instances locally for this schedule
            var futureInstances = await _connection.Table<ScheduledExercise>()
                .Where(se => se.RecurringScheduleId == scheduleId && se.ScheduledDate >= today && se.Status == "PENDING")
                .ToListAsync();

            foreach (var inst in futureInstances)
            {
                await _scheduledExerciseRepository.DeleteScheduledExerciseAsync(inst.Id);
            }

            return true;
        }

        public async Task ExtendWindowAsync(int userId)
        {
            var activeSchedules = await GetActiveSchedulesAsync(userId);
            var targetEndDate = DateTime.Today.AddDays(30);

            foreach (var schedule in activeSchedules)
            {
                if (schedule.LastGeneratedDate < targetEndDate)
                {
                    await GenerateOccurrencesAsync(schedule, targetEndDate).ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> DeleteInstanceAsync(int scheduledExerciseId, string scope)
        {
            var targetInstance = await _scheduledExerciseRepository.GetByIdAsync(scheduledExerciseId);
            if (targetInstance == null) return false;

            if (!targetInstance.RecurringScheduleId.HasValue || scope == "this_only")
            {
                return await _scheduledExerciseRepository.DeleteScheduledExerciseAsync(scheduledExerciseId);
            }

            int scheduleId = targetInstance.RecurringScheduleId.Value;
            var targetDate = targetInstance.ScheduledDate.Date;

            if (scope == "all")
            {
                return await CancelScheduleAsync(scheduleId);
            }
            else if (scope == "this_and_following")
            {
                var schedule = await GetByIdAsync(scheduleId);
                if (schedule != null)
                {
                    schedule.EndDate = targetDate.AddDays(-1);
                    if (schedule.EndDate < schedule.StartDate)
                    {
                        schedule.IsActive = false;
                    }

                    if (IsOnline())
                    {
                        try
                        {
                            var sModel = ToSupabase(schedule);
                            await _supabaseClient.From<SupabaseRecurringSchedule>()
                                .Update(sModel)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"DeleteInstanceAsync Supabase Error: {ex.Message}");
                        }
                    }
                    await _connection.InsertOrReplaceAsync(schedule).ConfigureAwait(false);
                }

                // Delete all pending instances from targetDate onwards for this schedule
                var matchingInstances = await _connection.Table<ScheduledExercise>()
                    .Where(se => se.RecurringScheduleId == scheduleId && se.ScheduledDate >= targetDate && se.Status == "PENDING")
                    .ToListAsync();

                foreach (var inst in matchingInstances)
                {
                    await _scheduledExerciseRepository.DeleteScheduledExerciseAsync(inst.Id);
                }
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateInstanceDateAsync(int scheduledExerciseId, DateTime newDate, string scope)
        {
            var targetInstance = await _scheduledExerciseRepository.GetByIdAsync(scheduledExerciseId);
            if (targetInstance == null) return false;

            if (!targetInstance.RecurringScheduleId.HasValue || scope == "this_only")
            {
                // Detach from recurring schedule and change date
                targetInstance.RecurringScheduleId = null;
                targetInstance.ScheduledDate = newDate.Date;
                return await _scheduledExerciseRepository.UpdateScheduledExerciseAsync(targetInstance);
            }

            int scheduleId = targetInstance.RecurringScheduleId.Value;
            var oldDate = targetInstance.ScheduledDate.Date;

            if (scope == "all")
            {
                // Shift all future instances by difference
                TimeSpan diff = newDate.Date - oldDate;
                var allFuture = await _connection.Table<ScheduledExercise>()
                    .Where(se => se.RecurringScheduleId == scheduleId && se.ScheduledDate >= oldDate && se.Status == "PENDING")
                    .ToListAsync();

                foreach (var inst in allFuture)
                {
                    inst.ScheduledDate = inst.ScheduledDate.Add(diff);
                    await _scheduledExerciseRepository.UpdateScheduledExerciseAsync(inst);
                }
                return true;
            }
            else if (scope == "this_and_following")
            {
                // Truncate current schedule up to oldDate - 1 day
                var schedule = await GetByIdAsync(scheduleId);
                if (schedule != null)
                {
                    schedule.EndDate = oldDate.AddDays(-1);
                    if (schedule.EndDate < schedule.StartDate)
                    {
                        schedule.IsActive = false;
                    }
                    await _connection.InsertOrReplaceAsync(schedule);
                    if (IsOnline())
                    {
                        try
                        {
                            await _supabaseClient.From<SupabaseRecurringSchedule>()
                                .Update(ToSupabase(schedule))
                                .ConfigureAwait(false);
                        }
                        catch { }
                    }
                }

                // Delete old pending instances >= oldDate
                var pendingToDelete = await _connection.Table<ScheduledExercise>()
                    .Where(se => se.RecurringScheduleId == scheduleId && se.ScheduledDate >= oldDate && se.Status == "PENDING")
                    .ToListAsync();

                foreach (var inst in pendingToDelete)
                {
                    await _scheduledExerciseRepository.DeleteScheduledExerciseAsync(inst.Id);
                }

                // Create a new schedule starting on newDate
                if (schedule != null)
                {
                    var newSchedule = new RecurringSchedule
                    {
                        UserId = schedule.UserId,
                        PatternType = schedule.PatternType,
                        DaysOfWeek = schedule.DaysOfWeek,
                        IntervalValue = schedule.IntervalValue,
                        IntervalUnit = schedule.IntervalUnit,
                        ExerciseIds = schedule.ExerciseIds,
                        StartDate = newDate.Date,
                        EndDate = null,
                        MaxOccurrences = schedule.MaxOccurrences,
                        IsActive = true
                    };
                    await CreateScheduleAsync(newSchedule);
                }
                return true;
            }

            return false;
        }

        private async Task GenerateOccurrencesAsync(RecurringSchedule schedule, DateTime targetEndDate)
        {
            DateTime startDate = schedule.LastGeneratedDate >= schedule.StartDate
                ? schedule.LastGeneratedDate.AddDays(1)
                : schedule.StartDate.Date;

            if (startDate > targetEndDate) return;

            DateTime? maxEnd = schedule.EndDate?.Date;

            List<DateTime> matchingDates = new List<DateTime>();
            var exerciseIdList = schedule.ExerciseIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (exerciseIdList.Count == 0) return;

            // Parse DaysOfWeek CSV if applicable
            HashSet<DayOfWeek>? targetDays = null;
            if (!string.IsNullOrEmpty(schedule.DaysOfWeek))
            {
                targetDays = schedule.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => (DayOfWeek)int.Parse(s))
                    .ToHashSet();
            }

            DateTime curr = startDate;
            while (curr <= targetEndDate)
            {
                if (maxEnd.HasValue && curr > maxEnd.Value) break;

                bool matches = false;
                switch (schedule.PatternType.ToLowerInvariant())
                {
                    case "daily":
                        matches = true;
                        break;

                    case "weekly":
                        if (curr.DayOfWeek == schedule.StartDate.DayOfWeek)
                        {
                            int weeks = (int)((curr - schedule.StartDate.Date).TotalDays / 7);
                            matches = (weeks % schedule.IntervalValue == 0);
                        }
                        break;

                    case "specific_days":
                        if (targetDays != null && targetDays.Contains(curr.DayOfWeek))
                        {
                            matches = true;
                        }
                        break;

                    case "every_n":
                        if (schedule.IntervalUnit.ToLowerInvariant() == "weeks")
                        {
                            if (curr.DayOfWeek == schedule.StartDate.DayOfWeek)
                            {
                                int weeks = (int)((curr - schedule.StartDate.Date).TotalDays / 7);
                                matches = (weeks % schedule.IntervalValue == 0);
                            }
                        }
                        else // "days"
                        {
                            int days = (int)(curr - schedule.StartDate.Date).TotalDays;
                            matches = (days >= 0 && days % schedule.IntervalValue == 0);
                        }
                        break;
                }

                if (matches)
                {
                    matchingDates.Add(curr);
                }

                curr = curr.AddDays(1);
            }

            // Build ScheduledExercise list
            List<ScheduledExercise> newExercises = new List<ScheduledExercise>();
            foreach (var date in matchingDates)
            {
                foreach (var exId in exerciseIdList)
                {
                    newExercises.Add(new ScheduledExercise
                    {
                        UserId = schedule.UserId,
                        ExerciseId = exId,
                        ScheduledDate = date,
                        Status = "PENDING",
                        RecurringScheduleId = schedule.Id,
                        IsSynced = false,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (newExercises.Count > 0)
            {
                await _scheduledExerciseRepository.AddScheduledExercisesAsync(newExercises).ConfigureAwait(false);
            }

            schedule.LastGeneratedDate = targetEndDate;
            await _connection.InsertOrReplaceAsync(schedule).ConfigureAwait(false);

            if (IsOnline())
            {
                try
                {
                    await _supabaseClient.From<SupabaseRecurringSchedule>()
                        .Update(ToSupabase(schedule))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Update LastGeneratedDate Supabase Error: {ex.Message}");
                }
            }
        }
    }
}
