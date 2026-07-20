namespace FitnessApp.Services
{
    public static class NutritionCalculator
    {
        public static double GetActivityMultiplier(string activityLevel)
        {
            return activityLevel switch
            {
                "Sedentary" => 1.2,
                "Lightly Active" => 1.375,
                "Moderately Active" => 1.55,
                "Very Active" => 1.725,
                _ => 1.55
            };
        }

        public static double CalculateBmr(double weightKg, double heightCm, int age, string gender)
        {
            if (weightKg <= 0 || heightCm <= 0 || age <= 0)
                return 0;

            // Mifflin-St Jeor formula:
            // Male / Others: 10 * weight(kg) + 6.25 * height(cm) - 5 * age + 5
            // Female: 10 * weight(kg) + 6.25 * height(cm) - 5 * age - 161
            if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
            {
                return (10 * weightKg) + (6.25 * heightCm) - (5 * age) - 161;
            }

            return (10 * weightKg) + (6.25 * heightCm) - (5 * age) + 5;
        }

        public static double CalculateTdee(double bmr, string activityLevel)
        {
            double multiplier = GetActivityMultiplier(activityLevel);
            return bmr * multiplier;
        }

        public static double CalculateCalories(double tdee, string goal)
        {
            if (tdee <= 0)
                return 2000;

            return goal switch
            {
                "Lose Weight" => tdee - 500,
                "Gain Weight" => tdee + 500,
                "Maintain" => tdee,
                "Just Track" => tdee,
                _ => tdee
            };
        }

        public static double CalculateWater(double weightKg)
        {
            if (weightKg <= 0)
                return 3000;

            // ~32.5 ml per kg of body weight
            return weightKg * 32.5;
        }
    }
}
