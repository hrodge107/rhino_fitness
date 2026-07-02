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

        [ObservableProperty]
        private ObservableCollection<BodyPartItem> _bodyParts = new();

        [ObservableProperty]
        private bool _isBusy;

        public WorkoutViewModel(
            INavigationService navigationService,
            IExerciseRepository repository) : base(navigationService)
        {
            _repository = repository;
            ActiveTab = "Workout";
        }

        [RelayCommand]
        public async Task LoadBodyPartsAsync()
        {
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
