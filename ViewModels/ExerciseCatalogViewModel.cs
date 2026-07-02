using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessApp.Models;
using FitnessApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FitnessApp.ViewModels
{
    public partial class ExerciseFilterChip : ObservableObject
    {
        public ExerciseFilterChip(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }

        public string Value { get; }

        [ObservableProperty]
        private bool _isSelected;
    }

    public partial class ExerciseCatalogViewModel : BaseViewModel
    {
        private readonly IExerciseRepository _repository;
        private readonly IDatabaseService _database;
        private readonly ILogger<ExerciseCatalogViewModel> _logger;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty]
        private ObservableCollection<Exercise> _exercises = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isSearchExpanded;

        [ObservableProperty]
        private ObservableCollection<ExerciseFilterChip> _categoryFilters = new();

        [ObservableProperty]
        private string _activeFilterCategory = "Muscle";

        [ObservableProperty]
        private string _emptyStateMessage = "No exercises match these filters.";

        public ExerciseCatalogViewModel(
            IExerciseRepository repository,
            IDatabaseService database,
            INavigationService navigationService,
            ILogger<ExerciseCatalogViewModel> logger) : base(navigationService)
        {
            _repository = repository;
            _database = database;
            _logger = logger;
            ActiveTab = "Catalog";
            HasSearch = true;

            if (!_database.IsSeedComplete)
            {
                IsBusy = true;
                _database.OnSeedCompleted += Database_OnSeedCompleted;
            }
        }

        private void Database_OnSeedCompleted(object? sender, EventArgs e)
        {
            _database.OnSeedCompleted -= Database_OnSeedCompleted;
            // The seed event fires on a background thread. Marshal to main thread to safely update UI state.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadExercisesAsync();
            });
        }

        [RelayCommand]
        public async Task LoadExercisesAsync()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            var token = _searchCts.Token;
            await LoadExercisesAsync(token);
        }

        private async Task LoadExercisesAsync(CancellationToken cancellationToken)
        {
            if (!_database.IsSeedComplete) return;

            try
            {
                IsBusy = true;

                if (CategoryFilters.Count == 0)
                {
                    if (ActiveFilterCategory == "Muscle")
                    {
                        var muscles = await _repository.GetUniqueMusclesAsync();
                        cancellationToken.ThrowIfCancellationRequested();

                        CategoryFilters = new ObservableCollection<ExerciseFilterChip>(
                            new[] { new ExerciseFilterChip("ALL MUSCLES", "All Muscles") { IsSelected = true } }
                                .Concat(muscles.Select(m => new ExerciseFilterChip(m.ToUpperInvariant(), m))));
                    }
                    else
                    {
                        var bodyParts = await _repository.GetUniqueBodyPartsAsync();
                        cancellationToken.ThrowIfCancellationRequested();

                        CategoryFilters = new ObservableCollection<ExerciseFilterChip>(
                            new[] { new ExerciseFilterChip("ALL BODY PARTS", "All Body Parts") { IsSelected = true } }
                                .Concat(bodyParts.Select(b => new ExerciseFilterChip(b.ToUpperInvariant(), b))));
                    }
                }

                string? selectedMuscle = null;
                string? selectedBodyPart = null;

                var selectedChip = CategoryFilters.FirstOrDefault(c => c.IsSelected)?.Value;
                if (ActiveFilterCategory == "Muscle")
                {
                    selectedMuscle = selectedChip ?? "All Muscles";
                }
                else
                {
                    selectedBodyPart = selectedChip ?? "All Body Parts";
                }

                var items = await _repository.GetFilteredExercisesAsync(SearchQuery, selectedMuscle, selectedBodyPart, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                Exercises = new ObservableCollection<Exercise>(items);
                EmptyStateMessage = items.Count == 0
                    ? "No exercises match these filters."
                    : string.Empty;
            }
            catch (OperationCanceledException)
            {
                // Swallowed: cancellation is expected when search query or filter changes rapidly.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading filtered exercises.");
                Exercises.Clear();
                EmptyStateMessage = "Error loading exercises. Please try again.";
            }
            finally
            {
                // Only reset IsBusy if this task wasn't cancelled. If cancelled,
                // another concurrent loading task will manage the Busy state.
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        public async Task SearchExercisesAsync()
        {
            await LoadExercisesAsync();
        }

        [RelayCommand]
        private void ToggleSearch()
        {
            IsSearchExpanded = !IsSearchExpanded;
            if (!IsSearchExpanded)
            {
                SearchQuery = string.Empty;
            }
        }

        [RelayCommand]
        private async Task SelectMuscle(ExerciseFilterChip chip)
        {
            foreach (var item in CategoryFilters)
            {
                item.IsSelected = ReferenceEquals(item, chip);
            }

            await LoadExercisesAsync();
        }

        [RelayCommand]
        private async Task SetFilterCategory(string category)
        {
            if (ActiveFilterCategory == category) return;
            ActiveFilterCategory = category;

            // Clear filters so they are re-queried for the new category
            CategoryFilters.Clear();
            await LoadExercisesAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            var token = _searchCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token);
                    if (token.IsCancellationRequested) return;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await LoadExercisesAsync(token);
                    });
                }
                catch (OperationCanceledException)
                {
                    // Swallowed
                }
            }, token);
        }
    }
}
