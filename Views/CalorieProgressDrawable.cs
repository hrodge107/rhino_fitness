using Microsoft.Maui.Graphics;

namespace FitnessApp.Views
{
    public class CalorieProgressDrawable : IDrawable
    {
        public double CurrentCalories { get; set; }
        public double CalorieLimit { get; set; } = 2000;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;

            // Calculate center and radius
            float margin = 10;
            float thickness = 12;
            float width = dirtyRect.Width - (margin * 2);
            float height = dirtyRect.Height - (margin * 2);
            float radius = Math.Min(width, height) / 2f;

            float centerX = dirtyRect.Width / 2f;
            float centerY = dirtyRect.Height / 2f;

            RectF arcRect = new RectF(centerX - radius, centerY - radius, radius * 2, radius * 2);

            // Draw background gray arc (rotated 270-degree arc: 135 to 45 degrees)
            canvas.StrokeColor = Color.FromArgb("#483E63"); // Dark purple track matching mock
            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Round;
            // DrawArc(x, y, w, h, startAngle, endAngle, clockwise, closed)
            canvas.DrawArc(arcRect.X, arcRect.Y, arcRect.Width, arcRect.Height, 135, 45, true, false);

            // Calculate progress percentage
            double percent = CalorieLimit > 0 ? CurrentCalories / CalorieLimit : 0;
            if (percent < 0) percent = 0;

            // Determine stroke color/gradient based on limit percentage
            Color progressColor;
            if (percent < 0.9)
            {
                progressColor = Color.FromArgb("#9B7FD4"); // Brand Purple (Light)
            }
            else if (percent <= 1.0)
            {
                progressColor = Color.FromArgb("#FFA500"); // Orange
            }
            else
            {
                progressColor = Color.FromArgb("#FF4D4D"); // Red
            }

            // Draw progress arc
            if (percent > 0)
            {
                // Sweep angle up to 270 degrees
                float endAngle = (float)(135 + (270 * Math.Min(percent, 1.0)));
                canvas.StrokeColor = progressColor;
                canvas.StrokeSize = thickness;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawArc(arcRect.X, arcRect.Y, arcRect.Width, arcRect.Height, 135, endAngle, true, false);
            }
        }
    }
}
