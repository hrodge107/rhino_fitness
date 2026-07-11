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

    [QueryProperty(nameof(BodyPart), "bodyPart")]
    public partial class ExerciseListViewModel : BaseViewModel
    {
        private readonly IExerciseRepository _repository;
        private readonly IScheduledExerciseRepository _scheduledExerciseRepository;
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

        public ExerciseListViewModel(
            INavigationService navigationService,
            IExerciseRepository repository,
            IScheduledExerciseRepository scheduledExerciseRepository,
            IPlannerStateService plannerStateService) : base(navigationService)
        {
            _repository = repository;
            _scheduledExerciseRepository = scheduledExerciseRepository;
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
            var selected = Exercises.Where(e => e.IsSelected).Select(e => new ScheduledExercise
            {
                UserId = _plannerStateService.CurrentUser?.Id ?? 1,
                ExerciseId = e.Exercise.ExerciseId,
                ScheduledDate = _plannerStateService.SelectedDate,
                Status = "PENDING"
            }).ToList();

            if (selected.Count == 0)
            {
                await Shell.Current.DisplayAlert("Info", "Please select at least one exercise.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var success = await _scheduledExerciseRepository.AddScheduledExercisesAsync(selected);
                if (success)
                {
                    // Absolute Shell navigation to PlannerPage to prevent duplicate additions
                    await NavigationService.GoToAsync("//PlannerPage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Could not add exercises. Please try again.", "OK");
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
