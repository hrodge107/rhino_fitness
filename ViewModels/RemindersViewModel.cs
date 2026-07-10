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

            var newReminder = new Reminder
            {
                UserId = user.Id,
                Category = SelectedCategory,
                Hour = SelectedTime.Hours,
                Minute = SelectedTime.Minutes
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
                else
                {
                    await Shell.Current.DisplayAlert("Duplicate", "A reminder for this category and time already exists.", "OK");
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
