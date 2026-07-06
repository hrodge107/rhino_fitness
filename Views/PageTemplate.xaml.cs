namespace FitnessApp.Views
{
    public partial class PageTemplate : ContentView
    {
        public static readonly BindableProperty PageTitleProperty =
            BindableProperty.Create(nameof(PageTitle), typeof(string), typeof(PageTemplate), string.Empty);

        public static readonly BindableProperty ShowBackArrowProperty =
            BindableProperty.Create(nameof(ShowBackArrow), typeof(bool), typeof(PageTemplate), true);

        public static readonly BindableProperty ShowFooterProperty =
            BindableProperty.Create(nameof(ShowFooter), typeof(bool), typeof(PageTemplate), true);

        public static readonly BindableProperty PageContentProperty =
            BindableProperty.Create(nameof(PageContent), typeof(View), typeof(PageTemplate));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public bool ShowBackArrow
        {
            get => (bool)GetValue(ShowBackArrowProperty);
            set => SetValue(ShowBackArrowProperty, value);
        }

        public bool ShowFooter
        {
            get => (bool)GetValue(ShowFooterProperty);
            set => SetValue(ShowFooterProperty, value);
        }

        public View PageContent
        {
            get => (View)GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        public PageTemplate()
        {
            InitializeComponent();
        }

        private async void OnBackTapped(object? sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
