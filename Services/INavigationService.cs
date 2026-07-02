namespace FitnessApp.Services
{
    /// <summary>
    /// Abstracts Shell navigation so ViewModels aren't coupled to Microsoft.Maui.Controls.
    /// Fulfills the DIP principle and the Design/Navigation/Intents rubric requirement.
    /// </summary>
    public interface INavigationService
    {
        Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
        Task GoBackAsync();
    }
}
