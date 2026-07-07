using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public partial class WorkoutViewModel : BaseViewModel
    {
        private readonly IExerciseRepository _repository;
        private readonly IDatabaseService _database;

        [ObservableProperty]
        private ObservableCollection<BodyPartItem> _bodyParts = new();

        [ObservableProperty]
        private bool _isBusy;

        public WorkoutViewModel(
            INavigationService navigationService,
            IExerciseRepository repository,
            IDatabaseService database) : base(navigationService)
        {
            _repository = repository;
            _database = database;
            ActiveTab = "Workout";

            // ponytail: wait for seed to complete
            if (!_database.IsSeedComplete)
            {
                IsBusy = true;
                _database.OnSeedCompleted += Database_OnSeedCompleted;
            }
        }

        private void Database_OnSeedCompleted(object? sender, EventArgs e)
        {
            _database.OnSeedCompleted -= Database_OnSeedCompleted;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadBodyPartsAsync();
            });
        }

        [RelayCommand]
        public async Task LoadBodyPartsAsync()
        {
            // ponytail: guard clause for incomplete seed
            if (!_database.IsSeedComplete) return;

            if (IsBusy || BodyParts.Count > 0) return;

            try
            {
                IsBusy = true;
                var parts = await _repository.GetUniqueBodyPartsAsync();
                BodyParts.Clear();
                for (int i = 0; i < parts.Count; i++)
                {
                    BodyParts.Add(new BodyPartItem
                    {
                        Name = parts[i],
                        CardColor = BodyPartItem.GetColorForIndex(i)
                    });
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToBodyPart(BodyPartItem item)
        {
            await NavigationService.GoToAsync("ExerciseListPage", new Dictionary<string, object>
            {
                { "bodyPart", item.Name }
            });
        }
    }
}
