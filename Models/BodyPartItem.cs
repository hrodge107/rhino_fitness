namespace FitnessApp.Models
{
    /// <summary>
    /// Lightweight display model for body part cards on the Workouts page.
    /// Color is assigned from a static purple palette by index.
    /// </summary>
    public class BodyPartItem
    {
        public string Name { get; set; } = string.Empty;
        public Color CardColor { get; set; } = Color.FromArgb("#5B2A9E");

        /// <summary>Title-cased display name.</summary>
        public string DisplayName => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Name);

        /// <summary>Purple cascade palette matching the brand screenshot.</summary>
        private static readonly string[] PurplePalette =
        {
            "#4A2080",  // darkest
            "#5B2A9E",
            "#6A35B8",
            "#7B45C8",
            "#8B5AD0",
            "#9B6FD8",
            "#A882DF",
            "#B595E5",
            "#C1A8EB",
            "#CCBBF0",  // lightest
        };

        public static Color GetColorForIndex(int index)
        {
            return Color.FromArgb(PurplePalette[index % PurplePalette.Length]);
        }
    }
}
