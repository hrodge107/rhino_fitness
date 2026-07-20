using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public class RecurringScheduleDisplayItem : ObservableObject
    {
        public int ScheduleId { get; set; }
        public string ExerciseNames { get; set; } = string.Empty;
        public string PatternDescription { get; set; } = string.Empty;
        public string DurationDescription { get; set; } = string.Empty;
        public string CreatedAtText { get; set; } = string.Empty;
    }

    public partial class RecurringSchedulesViewModel : BaseViewModel
    {
        private readonly IRecurringScheduleRepository _recurringScheduleRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IPlannerStateService _plannerStateService;

        private int UserId => _plannerStateService.CurrentUser?.Id
            ?? throw new InvalidOperationException("No authenticated user.");

        [ObservableProperty]
        private ObservableCollection<RecurringScheduleDisplayItem> _schedules = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasNoSchedules;

        [ObservableProperty]
        private bool _hasSchedules;

        public RecurringSchedulesViewModel(
            INavigationService navigationService,
            IRecurringScheduleRepository recurringScheduleRepository,
            IExerciseRepository exerciseRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _recurringScheduleRepository = recurringScheduleRepository;
            _exerciseRepository = exerciseRepository;
            _plannerStateService = plannerStateService;
        }

        [RelayCommand]
        public async Task LoadSchedulesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var rawSchedules = await _recurringScheduleRepository.GetActiveSchedulesAsync(UserId);
                var catalog = await _exerciseRepository.GetAllAsync();
                var catalogDict = catalog.ToDictionary(e => e.ExerciseId, e => e.Name);

                var displayItems = new List<RecurringScheduleDisplayItem>();

                foreach (var s in rawSchedules)
                {
                    // exercise names
                    var exIds = s.ExerciseIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var names = exIds.Select(id => catalogDict.TryGetValue(id, out var name) ? name : id).ToList();
                    string exerciseNamesText = string.Join(", ", names);

                    // Pattern Description
                    string patternDesc = FormatPattern(s);

                    // Duration Description
                    string durationDesc = FormatDuration(s);

                    displayItems.Add(new RecurringScheduleDisplayItem
                    {
                        ScheduleId = s.Id,
                        ExerciseNames = exerciseNamesText,
                        PatternDescription = patternDesc,
                        DurationDescription = durationDesc,
                        CreatedAtText = $"Created on {s.CreatedAt.ToLocalTime():MMM dd, yyyy}"
                    });
                }

                Schedules = new ObservableCollection<RecurringScheduleDisplayItem>(displayItems);
                HasNoSchedules = Schedules.Count == 0;
                HasSchedules = Schedules.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSchedulesAsync: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Could not load recurring schedules.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelSchedule(RecurringScheduleDisplayItem item)
        {
            if (item == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Cancel Recurring Schedule",
                $"Are you sure you want to stop the recurring schedule for {item.ExerciseNames}? Future entries will be removed.",
                "Yes, Cancel Schedule", "No");

            if (confirm)
            {
                try
                {
                    IsBusy = true;
                    bool success = await _recurringScheduleRepository.CancelScheduleAsync(item.ScheduleId);
                    if (success)
                    {
                        await LoadSchedulesAsync();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Could not cancel recurring schedule.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Could not cancel schedule: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private static string FormatPattern(RecurringSchedule s)
        {
            return s.PatternType.ToLowerInvariant() switch
            {
                "daily" => "Daily",
                "weekly" => $"Weekly on {s.StartDate.ToString("dddd")}",
                "specific_days" => FormatSpecificDays(s.DaysOfWeek),
                "every_n" => $"Every {s.IntervalValue} {s.IntervalUnit}",
                _ => s.PatternType
            };
        }

        private static string FormatSpecificDays(string? daysCsv)
        {
            if (string.IsNullOrEmpty(daysCsv)) return "Specific Days";

            var dayInts = daysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .Select(i => (DayOfWeek)i)
                .ToList();

            var names = dayInts.Select(d => d switch
            {
                DayOfWeek.Monday => "Mon",
                DayOfWeek.Tuesday => "Tue",
                DayOfWeek.Wednesday => "Wed",
                DayOfWeek.Thursday => "Thu",
                DayOfWeek.Friday => "Fri",
                DayOfWeek.Saturday => "Sat",
                DayOfWeek.Sunday => "Sun",
                _ => d.ToString()
            });

            return string.Join(", ", names);
        }

        private static string FormatDuration(RecurringSchedule s)
        {
            if (s.EndDate.HasValue)
            {
                return $"Ends on {s.EndDate.Value:MMM dd, yyyy}";
            }
            if (s.MaxOccurrences.HasValue)
            {
                return $"{s.MaxOccurrences.Value} occurrences limit";
            }
            return "No end date";
        }
    }
}
