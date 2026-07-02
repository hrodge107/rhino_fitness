namespace FitnessApp.Models
{
    public class DayModel
    {
        public string DayName { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public bool IsToday { get; set; }
    }
}
