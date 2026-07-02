using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    [QueryProperty(nameof(BodyPart), "bodyPart")]
    public partial class ExerciseListViewModel : BaseViewModel
    {
        private readonly IExerciseRepository _repository;

        [ObservableProperty]
        private string _bodyPart = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Exercises";

        [ObservableProperty]
        private ObservableCollection<Exercise> _exercises = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _emptyMessage = string.Empty;

        public ExerciseListViewModel(
            INavigationService navigationService,
            IExerciseRepository repository) : base(navigationService)
        {
            _repository = repository;
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
                Exercises = new ObservableCollection<Exercise>(items);
                EmptyMessage = items.Count == 0 ? "No exercises found for this body part." : string.Empty;
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
    }
}
