using FitnessApp.Models;

namespace FitnessApp.Services
{
    public interface IPlannerStateService
    {
        User? CurrentUser { get; set; }
        DateTime SelectedDate { get; set; }
    }
}
