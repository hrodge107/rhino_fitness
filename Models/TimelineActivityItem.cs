using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace FitnessApp.Models
{
    public class TimelineActivityItem
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Exercise", "Meal", "Water"
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DateText => Timestamp.ToString("MMMM d, yyyy");
        public Geometry? IconData { get; set; }
        public Brush ThemeColor { get; set; } = Brush.Default;
    }
}
