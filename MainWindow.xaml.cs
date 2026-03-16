using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LSBROWSER_CLEAN
{
    public partial class MainWindow : Window
    {
        private WebView2 webViewMinimal;
        private WebView2 webViewSidebar;

        private const string HomeUrl = "Homepage.html";   // Load local homepage
        private readonly string settingsPath = "settings.json";

        private bool isShiftAIVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebViews();
            LoadSettings();
            WireUpEvents();
        }

        private void InitializeWebViews()
        {
            // Minimal layout WebView2
            webViewMinimal = new WebView2();
            BrowserSurface_Minimal.Children.Add(webViewMinimal);
            webViewMinimal.NavigationCompleted += (s, e) =>
            {
                if (webViewMinimal.Source != null)
                    AddressBar.Text = webViewMinimal.Source.ToString();

                HomepageHost_Minimal.Visibility = Visibility.Collapsed;
            };

            // Sidebar layout WebView2
            webViewSidebar = new WebView2();
            BrowserSurface_Sidebar.Children.Add(webViewSidebar);
            webViewSidebar.NavigationCompleted += (s, e) =>
            {
                if (webViewSidebar.Source != null)
                    AddressBar.Text = webViewSidebar.Source.ToString();

                HomepageHost_Sidebar.Visibility = Visibility.Collapsed;
            };

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await webViewMinimal.EnsureCoreWebView2Async();
            await webViewSidebar.EnsureCoreWebView2Async();

            NavigateHome();
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    File.WriteAllText(settingsPath, JsonSerializer.Serialize(new SettingsModel { layout = "sidebar" }));
                }

                var json = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(settingsPath));
                ApplyLayout(json?.layout ?? "sidebar");
            }
            catch
            {
                ApplyLayout("sidebar");
            }
        }

        private void SaveSettings(string layout)
        {
            try
            {
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(new SettingsModel { layout = layout }));
            }
            catch
            {
                // ignore
            }
        }

        private void ApplyLayout(string mode)
        {
            if (mode == "minimal")
            {
                MinimalLayout.Visibility = Visibility.Visible;
                SidebarLayout.Visibility = Visibility.Collapsed;
            }
            else
            {
                MinimalLayout.Visibility = Visibility.Collapsed;
                SidebarLayout.Visibility = Visibility.Visible;
            }
        }

        private void WireUpEvents()
        {
            // Address bar navigation
            GoButton.Click += (s, e) => NavigateFromAddressBar();
            AddressBar.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    NavigateFromAddressBar();
            };

            // Navigation buttons
            BackButton.Click += (s, e) =>
            {
                var wv = GetActiveWebView();
                if (wv != null && wv.CanGoBack)
                    wv.GoBack();
            };

            ForwardButton.Click += (s, e) =>
            {
                var wv = GetActiveWebView();
                if (wv != null && wv.CanGoForward)
                    wv.GoForward();
            };

            ReloadButton.Click += (s, e) =>
            {
                var wv = GetActiveWebView();
                wv?.Reload();
            };

            // Homepage search boxes
            HomepageSearch_Minimal.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    AddressBar.Text = HomepageSearch_Minimal.Text;
                    NavigateFromAddressBar();
                }
            };

            HomepageSearch_Sidebar.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    AddressBar.Text = HomepageSearch_Sidebar.Text;
                    NavigateFromAddressBar();
                }
            };

            // Sidebar buttons
            SidebarHome.Click += (s, e) => NavigateHome();
            SidebarTabs.Click += (s, e) => { /* future tab UI */ };
            SidebarHistory.Click += (s, e) => { /* future history UI */ };
            SidebarShiftAI.Click += (s, e) => ToggleShiftAI();
            SidebarSettings.Click += (s, e) => OpenSettings();

            // ShiftAI pill
            ShiftAIPill.MouseLeftButtonUp += (s, e) => ToggleShiftAI();

            // Window drag
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private WebView2 GetActiveWebView()
        {
            if (SidebarLayout.Visibility == Visibility.Visible)
                return webViewSidebar;

            return webViewMinimal;
        }

        private void NavigateFromAddressBar()
        {
            string url = AddressBar.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            var wv = GetActiveWebView();
            if (wv?.CoreWebView2 != null)
                wv.CoreWebView2.Navigate(url);
        }

        private void NavigateHome()
        {
            var wv = GetActiveWebView();
            if (wv?.CoreWebView2 != null)
            {
                string fullPath = Path.GetFullPath(HomeUrl);
                wv.CoreWebView2.Navigate(fullPath);
            }

            HomepageHost_Minimal.Visibility = Visibility.Visible;
            HomepageHost_Sidebar.Visibility = Visibility.Visible;
        }

        private void ToggleShiftAI()
        {
            isShiftAIVisible = !isShiftAIVisible;
            ShiftAIPanel.Visibility = isShiftAIVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenSettings()
        {
            string newLayout = SidebarLayout.Visibility == Visibility.Visible ? "minimal" : "sidebar";
            ApplyLayout(newLayout);
            SaveSettings(newLayout);
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class SettingsModel
    {
        public string layout { get; set; }
    }
}
