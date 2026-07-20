using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;
using Microsoft.Maui.Controls;

namespace FitnessApp.ViewModels
{
    public partial class RemindersViewModel : BaseViewModel
    {
        private readonly INotificationScheduler _notificationScheduler;
        private readonly IReminderRepository _reminderRepository;
        private readonly IPlannerStateService _plannerStateService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowPermissionBanner))]
        [NotifyPropertyChangedFor(nameof(PermissionStatusText))]
        [NotifyPropertyChangedFor(nameof(PermissionStatusColor))]
        private bool _hasNotificationPermission = true;

        public bool ShowPermissionBanner => !HasNotificationPermission;
        public string PermissionStatusText => HasNotificationPermission ? "Granted" : "Denied";
        public Color PermissionStatusColor => HasNotificationPermission ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");

        public List<string> Categories { get; } = new()
        {
            "Exercise",
            "Breakfast",
            "Lunch",
            "Dinner",
            "Water",
            "Snack"
        };

        [ObservableProperty]
        private string _selectedCategory = "Exercise";

        [ObservableProperty]
        private TimeSpan _selectedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWeeklyPattern))]
        [NotifyPropertyChangedFor(nameof(IsCustomIntervalPattern))]
        [NotifyPropertyChangedFor(nameof(DailyBgColor))]
        [NotifyPropertyChangedFor(nameof(WeeklyBgColor))]
        [NotifyPropertyChangedFor(nameof(CustomIntervalBgColor))]
        private string _selectedRecurrenceType = "daily"; // "daily", "weekly", "custom_interval"

        [ObservableProperty]
        private int _intervalValue = 1;

        [ObservableProperty]
        private string _intervalUnit = "days"; // "days", "weeks"

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddDays(7);

        [ObservableProperty]
        private TimeSpan _endTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

        public bool IsWeeklyPattern => SelectedRecurrenceType == "weekly";
        public bool IsCustomIntervalPattern => SelectedRecurrenceType == "custom_interval";

        public string DailyBgColor => SelectedRecurrenceType == "daily" ? "#5B2A9E" : "#2A2A2E";
        public string WeeklyBgColor => SelectedRecurrenceType == "weekly" ? "#5B2A9E" : "#2A2A2E";
        public string CustomIntervalBgColor => SelectedRecurrenceType == "custom_interval" ? "#5B2A9E" : "#2A2A2E";

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

        public ObservableCollection<Reminder> Reminders { get; } = new();

        public RemindersViewModel(
            INavigationService navigationService,
            INotificationScheduler notificationScheduler,
            IReminderRepository reminderRepository,
            IPlannerStateService plannerStateService) 
            : base(navigationService)
        {
            _notificationScheduler = notificationScheduler;
            _reminderRepository = reminderRepository;
            _plannerStateService = plannerStateService;
        }

        [RelayCommand]
        private void SelectRecurrenceType(string type)
        {
            SelectedRecurrenceType = type;
        }

        [RelayCommand]
        private void ToggleDay(DayOfWeekChip chip)
        {
            if (chip != null)
            {
                chip.IsSelected = !chip.IsSelected;
            }
        }

        [RelayCommand]
        public async Task CheckPermissionsAsync()
        {
            HasNotificationPermission = await _notificationScheduler.CheckPermissionsAsync();
        }

        [RelayCommand]
        public async Task RequestPermissionsAsync()
        {
            HasNotificationPermission = await _notificationScheduler.RequestPermissionsAsync();
        }

        [RelayCommand]
        public async Task OpenSettingsAsync()
        {
            await _notificationScheduler.OpenSettingsAsync();
        }

        [RelayCommand]
        public async Task LoadRemindersAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null) return;

            IsBusy = true;
            try
            {
                var list = await _reminderRepository.GetRemindersForUserAsync(user.Id);
                Reminders.Clear();
                foreach (var r in list)
                {
                    Reminders.Add(r);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] RemindersViewModel LoadRemindersAsync failed: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task AddReminderAsync()
        {
            var user = _plannerStateService.CurrentUser;
            if (user == null) return;

            if (string.IsNullOrWhiteSpace(SelectedCategory)) return;

            string? daysCsv = null;
            DateTime? finalEndDate = null;

            if (SelectedRecurrenceType == "weekly")
            {
                var selectedDays = WeekDays.Where(d => d.IsSelected).Select(d => d.DisplayName).ToList();
                if (!selectedDays.Any())
                {
                    await Shell.Current.DisplayAlert("Select Days", "Please select at least one day of the week for weekly recurrence.", "OK");
                    return;
                }
                daysCsv = string.Join(", ", selectedDays);
            }
            else if (SelectedRecurrenceType == "custom_interval")
            {
                finalEndDate = EndDate.Date.Add(EndTime);
                if (finalEndDate <= DateTime.Now)
                {
                    await Shell.Current.DisplayAlert("Required End Date", "Custom Interval schedules require a future end date and time.", "OK");
                    return;
                }
            }

            var newReminder = new Reminder
            {
                UserId = user.Id,
                Category = SelectedCategory,
                Hour = SelectedTime.Hours,
                Minute = SelectedTime.Minutes,
                RecurrenceType = SelectedRecurrenceType,
                DaysOfWeek = daysCsv,
                IntervalValue = Math.Max(1, IntervalValue),
                IntervalUnit = string.IsNullOrWhiteSpace(IntervalUnit) ? "days" : IntervalUnit,
                StartDate = StartDate.Date.Add(SelectedTime),
                EndDate = finalEndDate
            };

            IsBusy = true;
            try
            {
                var success = await _reminderRepository.AddReminderAsync(newReminder);
                if (success)
                {
                    await _notificationScheduler.ScheduleReminderAsync(newReminder);
                    await LoadRemindersAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteReminderAsync(Reminder reminder)
        {
            if (reminder == null) return;

            IsBusy = true;
            try
            {
                var success = await _reminderRepository.DeleteReminderAsync(reminder.Id);
                if (success)
                {
                    await _notificationScheduler.CancelReminderAsync(reminder);
                    await LoadRemindersAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                await CheckPermissionsAsync();
                if (!HasNotificationPermission)
                {
                    await RequestPermissionsAsync();
                }
                await LoadRemindersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] RemindersViewModel InitializeAsync failed: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
