namespace FitnessApp.Services
{
    /// <summary>
    /// Implements INavigationService by delegating to Shell.Current.
    /// Registered as a singleton in MauiProgram.cs.
    /// </summary>
    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (parameters != null)
            {
                return Shell.Current.GoToAsync(route, parameters);
            }
            return Shell.Current.GoToAsync(route);
        }

        public Task GoBackAsync()
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                return Shell.Current.GoToAsync("..");
            }
            return Shell.Current.GoToAsync("//HomePage");
        }
    }
}
