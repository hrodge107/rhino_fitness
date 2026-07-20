using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public class SelectableExercise : ObservableObject
    {
        public Exercise Exercise { get; set; } = null!;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public partial class DayOfWeekChip : ObservableObject
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChipBgColor))]
        [NotifyPropertyChangedFor(nameof(ChipBorderColor))]
        private bool _isSelected;

        public string ChipBgColor => IsSelected ? "#5B2A9E" : "#2A2A2E";
        public string ChipBorderColor => IsSelected ? "#9B7FD4" : "#3A3A40";

        public DayOfWeekChip(DayOfWeek day, string name, bool selected = false)
        {
            DayOfWeek = day;
            DisplayName = name;
            IsSelected = selected;
        }
    }

    [QueryProperty(nameof(BodyPart), "bodyPart")]
    public partial class ExerciseListViewModel : BaseViewModel
    {
        private readonly IExerciseRepository _repository;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IRecurringScheduleRepository _recurringScheduleRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        private string _bodyPart = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Exercises";

        [ObservableProperty]
        private ObservableCollection<SelectableExercise> _exercises = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _emptyMessage = string.Empty;

        [ObservableProperty]
        private bool _hasSelectedExercises;

        // Recurrence properties
        [ObservableProperty]
        private bool _isRecurring;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSpecificDaysPattern))]
        [NotifyPropertyChangedFor(nameof(IsEveryNPattern))]
        [NotifyPropertyChangedFor(nameof(DailyBgColor))]
        [NotifyPropertyChangedFor(nameof(WeeklyBgColor))]
        [NotifyPropertyChangedFor(nameof(SpecificDaysBgColor))]
        [NotifyPropertyChangedFor(nameof(EveryNBgColor))]
        private string _patternType = "daily"; // "daily", "weekly", "specific_days", "every_n"

        [ObservableProperty]
        private int _intervalValue = 1;

        [ObservableProperty]
        private string _intervalUnit = "days"; // "days", "weeks"

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEndDate))]
        [NotifyPropertyChangedFor(nameof(IsOccurrencesCount))]
        [NotifyPropertyChangedFor(nameof(NoEndBgColor))]
        [NotifyPropertyChangedFor(nameof(EndDateBgColor))]
        [NotifyPropertyChangedFor(nameof(OccurrencesBgColor))]
        private string _endType = "no_end"; // "no_end", "end_date", "occurrences"

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddMonths(1);

        [ObservableProperty]
        private int _occurrenceCount = 10;

        // Pattern visibility helpers
        public bool IsSpecificDaysPattern => PatternType == "specific_days";
        public bool IsEveryNPattern => PatternType == "every_n";
        public bool IsEndDate => EndType == "end_date";
        public bool IsOccurrencesCount => EndType == "occurrences";

        // Pattern button background colors
        public string DailyBgColor => PatternType == "daily" ? "#5B2A9E" : "#2A2A2E";
        public string WeeklyBgColor => PatternType == "weekly" ? "#5B2A9E" : "#2A2A2E";
        public string SpecificDaysBgColor => PatternType == "specific_days" ? "#5B2A9E" : "#2A2A2E";
        public string EveryNBgColor => PatternType == "every_n" ? "#5B2A9E" : "#2A2A2E";

        // End type button background colors
        public string NoEndBgColor => EndType == "no_end" ? "#5B2A9E" : "#2A2A2E";
        public string EndDateBgColor => EndType == "end_date" ? "#5B2A9E" : "#2A2A2E";
        public string OccurrencesBgColor => EndType == "occurrences" ? "#5B2A9E" : "#2A2A2E";

        public ObservableCollection<DayOfWeekChip> WeekDays { get; } = new()
        {
            new DayOfWeekChip(DayOfWeek.Monday, "Mon", true),
            new DayOfWeekChip(DayOfWeek.Tuesday, "Tue", false),
            new DayOfWeekChip(DayOfWeek.Wednesday, "Wed", true),
            new DayOfWeekChip(DayOfWeek.Thursday, "Thu", false),
            new DayOfWeekChip(DayOfWeek.Friday, "Fri", true),
            new DayOfWeekChip(DayOfWeek.Saturday, "Sat", false),
            new DayOfWeekChip(DayOfWeek.Sunday, "Sun", false)
        };

        public ExerciseListViewModel(
            INavigationService navigationService,
            IExerciseRepository repository,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IRecurringScheduleRepository recurringScheduleRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _repository = repository;
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _recurringScheduleRepository = recurringScheduleRepository;
            _plannerStateService = plannerStateService;
        }

        partial void OnBodyPartChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                PageTitle = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
                _ = LoadExercisesAsync();
            }
        }

        [RelayCommand]
        private void SelectPattern(string pattern)
        {
            PatternType = pattern;
        }

        [RelayCommand]
        private void SelectEndType(string type)
        {
            EndType = type;
        }

        [RelayCommand]
        private void ToggleDay(DayOfWeekChip day)
        {
            if (day != null)
            {
                day.IsSelected = !day.IsSelected;
            }
        }

        [RelayCommand]
        public async Task LoadExercisesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var items = await _repository.GetFilteredExercisesAsync(null, null, BodyPart);

                var selectable = items.Select(i => new SelectableExercise 
                { 
                    Exercise = i, 
                    IsSelected = false 
                }).ToList();

                // Wire up property changed event to update HasSelectedExercises
                foreach (var s in selectable)
                {
                    s.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(SelectableExercise.IsSelected))
                        {
                            HasSelectedExercises = Exercises.Any(e => e.IsSelected);
                        }
                    };
                }

                Exercises = new ObservableCollection<SelectableExercise>(selectable);
                EmptyMessage = items.Count == 0 ? "No exercises found for this body part." : string.Empty;
                HasSelectedExercises = false;
            }
            catch (Exception)
            {
                Exercises.Clear();
                EmptyMessage = "Error loading exercises.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddSelected()
        {
            var selectedExercises = Exercises.Where(e => e.IsSelected).ToList();

            if (selectedExercises.Count == 0)
            {
                await Shell.Current.DisplayAlert("Info", "Please select at least one exercise.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                int userId = _plannerStateService.CurrentUser?.Id ?? 1;

                if (IsRecurring)
                {
                    var exerciseIds = string.Join(",", selectedExercises.Select(e => e.Exercise.ExerciseId));
                    var selectedDaysCsv = string.Join(",", WeekDays.Where(w => w.IsSelected).Select(w => (int)w.DayOfWeek));

                    DateTime? calculatedEndDate = null;
                    int? calculatedMaxOccurrences = null;

                    if (EndType == "end_date")
                    {
                        calculatedEndDate = EndDate.Date;
                    }
                    else if (EndType == "occurrences")
                    {
                        calculatedMaxOccurrences = OccurrenceCount;
                    }

                    var schedule = new RecurringSchedule
                    {
                        UserId = userId,
                        PatternType = PatternType,
                        DaysOfWeek = PatternType == "specific_days" ? selectedDaysCsv : null,
                        IntervalValue = IntervalValue,
                        IntervalUnit = IntervalUnit,
                        ExerciseIds = exerciseIds,
                        StartDate = _plannerStateService.SelectedDate.Date,
                        EndDate = calculatedEndDate,
                        MaxOccurrences = calculatedMaxOccurrences,
                        IsActive = true
                    };

                    var success = await _recurringScheduleRepository.CreateScheduleAsync(schedule);
                    if (success)
                    {
                        await NavigationService.GoToAsync("//PlannerPage");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Could not create recurring schedule. Please try again.", "OK");
                    }
                }
                else
                {
                    var selected = selectedExercises.Select(e => new ScheduledExercise
                    {
                        UserId = userId,
                        ExerciseId = e.Exercise.ExerciseId,
                        ScheduledDate = _plannerStateService.SelectedDate,
                        Status = "PENDING"
                    }).ToList();

                    var success = await _scheduledExerciseRepository.AddScheduledExercisesAsync(selected);
                    if (success)
                    {
                        await NavigationService.GoToAsync("//PlannerPage");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Could not add exercises. Please try again.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Could not add exercises: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
