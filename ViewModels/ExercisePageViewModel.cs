using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    [QueryProperty(nameof(ScheduledExerciseIdString), "scheduledExerciseId")]
    public partial class ExercisePageViewModel : BaseViewModel
    {
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IDispatcherTimer _elapsedTimer;
        private readonly IDispatcherTimer _restTimer;

        private int _scheduledExerciseId;
        private ScheduledExercise? _scheduledExercise;

        [ObservableProperty]
        private string _scheduledExerciseIdString = string.Empty;

        [ObservableProperty]
        private Exercise? _exercise;

        [ObservableProperty]
        private FormattedString _formattedInstructions = new();

        [ObservableProperty]
        private bool _isSessionActive;

        [ObservableProperty]
        private bool _isSessionInactive = true;



        [ObservableProperty]
        private string _elapsedTimerText = "00:00";

        [ObservableProperty]
        private string _restTimerText = "00:30";

        [ObservableProperty]
        private bool _isResting;

        [ObservableProperty]
        private int _completedSets;

        [ObservableProperty]
        private int _targetRestDuration = 30; // default 30s

        private int _elapsedSeconds;
        private int _restSecondsLeft;

        public ExercisePageViewModel(
            INavigationService navigationService,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IExerciseRepository exerciseRepository) : base(navigationService)
        {
            _scheduledExerciseRepository = scheduledExerciseRepository;
            _exerciseRepository = exerciseRepository;

            // Session timer
            _elapsedTimer = Application.Current!.Dispatcher.CreateTimer();
            _elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            _elapsedTimer.Tick += ElapsedTimer_Tick;

            // Rest timer
            _restTimer = Application.Current!.Dispatcher.CreateTimer();
            _restTimer.Interval = TimeSpan.FromSeconds(1);
            _restTimer.Tick += RestTimer_Tick;
        }

        partial void OnScheduledExerciseIdStringChanged(string value)
        {
            if (int.TryParse(value, out int id))
            {
                _scheduledExerciseId = id;
                _ = LoadExerciseAsync();
            }
        }

        partial void OnExerciseChanged(Exercise? value)
        {
            if (value == null || string.IsNullOrEmpty(value.Instructions))
            {
                FormattedInstructions = new FormattedString();
                return;
            }

            var regex = new System.Text.RegularExpressions.Regex(@"(Step:\d+)");
            var parts = regex.Split(value.Instructions);
            var formatted = new FormattedString();

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part)) continue;

                if (regex.IsMatch(part))
                {
                    formatted.Spans.Add(new Span
                    {
                        Text = part,
                        FontFamily = "OpenSansSemibold",
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Microsoft.Maui.Graphics.Colors.White
                    });
                }
                else
                {
                    string text = part;
                    if (text.EndsWith("\n"))
                    {
                        text = text.TrimEnd('\r', '\n') + "\n\n";
                    }
                    else if (text.Contains("\n"))
                    {
                        text = text.Replace("\n", "\n\n");
                    }

                    formatted.Spans.Add(new Span
                    {
                        Text = text,
                        FontFamily = "OpenSansRegular",
                        TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#D0D0D5")
                    });
                }
            }

            FormattedInstructions = formatted;
        }

        private async Task LoadExerciseAsync()
        {
            IsBusy = true;
            try
            {
                int userId = 1; // active user
                var scheduled = await _scheduledExerciseRepository.GetScheduledExercisesForDateAsync(userId, DateTime.Today);
                // Query via repo to support sync
                _scheduledExercise = await _scheduledExerciseRepository.GetByIdAsync(_scheduledExerciseId);

                if (_scheduledExercise != null)
                {
                    var catalog = await _exerciseRepository.GetAllAsync();
                    Exercise = catalog.FirstOrDefault(e => e.ExerciseId == _scheduledExercise.ExerciseId);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Could not load exercise: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void StartWorkout()
        {
            IsSessionActive = true;
            IsSessionInactive = false;
            _elapsedSeconds = 0;
            ElapsedTimerText = "00:00";
            _elapsedTimer.Start();

            // Setup rest duration
            _restSecondsLeft = TargetRestDuration;
            RestTimerText = FormatTime(_restSecondsLeft);
        }

        [RelayCommand]
        private void IncrementSet()
        {
            CompletedSets++;
        }

        [RelayCommand]
        private void DecrementSet()
        {
            if (CompletedSets > 0)
            {
                CompletedSets--;
            }
        }

        [RelayCommand]
        private void StartRest()
        {
            if (IsResting)
            {
                // Toggle resting state
                _restTimer.Stop();
                IsResting = false;
            }
            else
            {
                if (_restSecondsLeft <= 0)
                {
                    _restSecondsLeft = TargetRestDuration;
                }
                RestTimerText = FormatTime(_restSecondsLeft);
                _restTimer.Start();
                IsResting = true;
            }
        }

        [RelayCommand]
        private void ChangeRestDuration(string amountStr)
        {
            if (int.TryParse(amountStr, out int amount))
            {
                int newDuration = TargetRestDuration + amount;
                if (newDuration < 5) newDuration = 5; // min 5s
                TargetRestDuration = newDuration;

                if (!IsResting)
                {
                    _restSecondsLeft = TargetRestDuration;
                    RestTimerText = FormatTime(_restSecondsLeft);
                }
            }
        }

        [RelayCommand]
        private void SetRestPreset(string presetStr)
        {
            if (int.TryParse(presetStr, out int preset))
            {
                TargetRestDuration = preset;
                if (!IsResting)
                {
                    _restSecondsLeft = TargetRestDuration;
                    RestTimerText = FormatTime(_restSecondsLeft);
                }
            }
        }

        [RelayCommand]
        private async Task ResetSession()
        {
            bool confirm = await Shell.Current.DisplayAlert("Reset Exercise", "Are you sure you want to reset this session?", "Yes", "No");
            if (!confirm) return;

            _elapsedTimer.Stop();
            _restTimer.Stop();
            
            IsSessionActive = false;
            IsSessionInactive = true;
            IsResting = false;
            CompletedSets = 0;
            _elapsedSeconds = 0;
            _restSecondsLeft = TargetRestDuration;
            
            ElapsedTimerText = "00:00";
            RestTimerText = FormatTime(TargetRestDuration);
        }

        [RelayCommand]
        private async Task FinishSession()
        {
            // Confirm completion
            bool confirm = await Shell.Current.DisplayAlert("Complete Exercise", "Are you sure you want to finish this session?", "Yes", "No");
            if (!confirm) return;

            if (_scheduledExercise != null)
            {
                IsBusy = true;
                try
                {
                    _scheduledExercise.Status = "COMPLETED";
                    _scheduledExercise.Sets = CompletedSets;
                    _scheduledExercise.DurationSeconds = _elapsedSeconds;
                    _scheduledExercise.UpdatedAt = DateTime.UtcNow;
                    
                    var success = await _scheduledExerciseRepository.UpdateScheduledExerciseAsync(_scheduledExercise);
                    if (!success)
                    {
                        await Shell.Current.DisplayAlert("Error", "Network disconnected. Could not save changes.", "OK");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Could not save workout status: {ex.Message}", "OK");
                    return;
                }
                finally
                {
                    IsBusy = false;
                }
            }

            _elapsedTimer.Stop();
            _restTimer.Stop();

            // Prevent re-submissions
            await NavigationService.GoToAsync("//PlannerPage");
        }

        public void CleanUp()
        {
            _elapsedTimer.Stop();
            _restTimer.Stop();
        }

        private void ElapsedTimer_Tick(object? sender, EventArgs e)
        {
            _elapsedSeconds++;
            ElapsedTimerText = FormatTime(_elapsedSeconds);
        }

        private void RestTimer_Tick(object? sender, EventArgs e)
        {
            if (_restSecondsLeft > 0)
            {
                _restSecondsLeft--;
                RestTimerText = FormatTime(_restSecondsLeft);
            }
            else
            {
                _restTimer.Stop();
                IsResting = false;
                // Alert finished
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Rest Finished", "Time to start your next set!", "OK");
                });
            }
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
