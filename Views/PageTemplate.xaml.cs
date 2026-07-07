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

        public static readonly BindableProperty OfflineBannerVisibleProperty =
            BindableProperty.Create(nameof(OfflineBannerVisible), typeof(bool), typeof(PageTemplate), false);

        public bool OfflineBannerVisible
        {
            get => (bool)GetValue(OfflineBannerVisibleProperty);
            set => SetValue(OfflineBannerVisibleProperty, value);
        }

        public static readonly BindableProperty IsBusyProperty =
            BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(PageTemplate), false);

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public PageTemplate()
        {
            InitializeComponent();
            Loaded += PageTemplate_Loaded;
            Unloaded += PageTemplate_Unloaded;
        }

        private void PageTemplate_Loaded(object? sender, EventArgs e)
        {
            Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
            UpdateConnectivity();
        }

        private void PageTemplate_Unloaded(object? sender, EventArgs e)
        {
            Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object? sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
        {
            UpdateConnectivity();
        }

        private void UpdateConnectivity()
        {
            OfflineBannerVisible = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet;
        }

        private async void OnBackTapped(object? sender, TappedEventArgs e)
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("//HomePage");
            }
        }
    }
}
