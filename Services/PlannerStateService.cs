using FitnessApp.Models;

namespace FitnessApp.Services
{
    public class PlannerStateService : IPlannerStateService
    {
        public User? CurrentUser { get; set; }
        
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => _selectedDate = value.Date;
        }
    }
}
