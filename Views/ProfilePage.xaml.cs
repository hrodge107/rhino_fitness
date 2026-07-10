using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _vm;
        private string _activeFilter = "All";
        private readonly Easing _elasticEasing;

        public ProfilePage(ProfileViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;

            _elasticEasing = GetCubicBezier(0.65, 0, 0.35, 1);

            TabsGrid.SizeChanged += (s, e) =>
            {
                double segmentWidth = TabsGrid.Width / 4;
                CapsuleHighlight.WidthRequest = segmentWidth;
                int index = GetIndexForFilter(_activeFilter);
                CapsuleHighlight.TranslationX = index * segmentWidth;
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadProfileAsync();
        }

        private static Easing GetCubicBezier(double x1, double y1, double x2, double y2)
        {
            return new Easing(t =>
            {
                if (t <= 0) return 0;
                if (t >= 1) return 1;
                double u = t;
                for (int i = 0; i < 8; i++)
                {
                    double x = 3 * (1 - u) * (1 - u) * u * x1 + 3 * (1 - u) * u * u * x2 + u * u * u;
                    double dx = 3 * (1 - u) * (1 - u) * x1 + 6 * (1 - u) * u * (x2 - x1) + 3 * u * u * (1 - x2);
                    if (Math.Abs(dx) < 1e-6) break;
                    u -= (x - t) / dx;
                }
                return 3 * (1 - u) * (1 - u) * u * y1 + 3 * (1 - u) * u * u * y2 + u * u * u;
            });
        }

        private int GetIndexForFilter(string filter) => filter switch
        {
            "All" => 0,
            "Exercises" => 1,
            "Meal" => 2,
            "Water" => 3,
            _ => 0
        };

        private Label GetLabelForFilter(string filter) => filter switch
        {
            "All" => TabAll,
            "Exercises" => TabExercises,
            "Meal" => TabMeal,
            "Water" => TabWater,
            _ => TabAll
        };

        private async void OnTabTapped(object sender, TappedEventArgs e)
        {
            string targetFilter = e.Parameter as string ?? "All";
            if (targetFilter == _activeFilter) return;

            string oldFilter = _activeFilter;
            _activeFilter = targetFilter;

            AnimateTabTransition(oldFilter, targetFilter);

            // SetFilterAsync will be added in VM in later steps, define placeholder if needed
            await _vm.SetFilterAsync(targetFilter);
        }

        private void AnimateTabTransition(string oldFilter, string newFilter)
        {
            int oldIndex = GetIndexForFilter(oldFilter);
            int newIndex = GetIndexForFilter(newFilter);
            double segmentWidth = TabsGrid.Width / 4;

            double startX = oldIndex * segmentWidth;
            double targetX = newIndex * segmentWidth;

            var oldLabel = GetLabelForFilter(oldFilter);
            var newLabel = GetLabelForFilter(newFilter);

            this.AbortAnimation("TabTransition");

            var inactiveColor = Color.FromArgb("D0D0D5");
            var activeColor = Colors.White;

            var transitionAnim = new Animation();

            var transAnim = new Animation(v => CapsuleHighlight.TranslationX = v, startX, targetX);
            transitionAnim.Add(0, 1, transAnim);

            var oldColorAnim = new Animation(v => {
                oldLabel.TextColor = Color.FromRgb(
                    activeColor.Red + (inactiveColor.Red - activeColor.Red) * v,
                    activeColor.Green + (inactiveColor.Green - activeColor.Green) * v,
                    activeColor.Blue + (inactiveColor.Blue - activeColor.Blue) * v
                );
            }, 0, 1);
            transitionAnim.Add(0, 1, oldColorAnim);

            var newColorAnim = new Animation(v => {
                newLabel.TextColor = Color.FromRgb(
                    inactiveColor.Red + (activeColor.Red - inactiveColor.Red) * v,
                    inactiveColor.Green + (activeColor.Green - inactiveColor.Green) * v,
                    inactiveColor.Blue + (activeColor.Blue - inactiveColor.Blue) * v
                );
            }, 0, 1);
            transitionAnim.Add(0, 1, newColorAnim);

            transitionAnim.Commit(this, "TabTransition", 16, 400, _elasticEasing);
        }
    }
}
