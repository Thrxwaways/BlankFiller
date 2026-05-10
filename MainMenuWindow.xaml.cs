using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BlankFiller.Models;
using BlankFiller.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace BlankFiller.Views;

public partial class MainMenuWindow : Window
{
    private readonly string _inspectorName;
    private readonly string _inspectorTitleField;
    private readonly string _medicField;
    private bool _sidebarCollapsed = false;
    private List<Button> _expandableButtons = new();
    private DispatcherTimer _clockTimer;

    public MainMenuWindow(string inspectorName, string inspectorTitle, string medic)
    {
        InitializeComponent();
        _inspectorName = inspectorName;
        _inspectorTitleField = inspectorTitle;
        _medicField = medic;

        var formData = App.Host!.Services.GetRequiredService<BlankFiller.Services.FormDataService>();
        formData.Set("CurrentMedic", medic ?? "");
        formData.Set("CurrentInspectorFullName", inspectorName);

        InspectorNameText.Text = inspectorName;
        MedicNameText.Text = medic;

        var nameParts = inspectorName.Split(' ');
        var shortName = nameParts.Length >= 3
            ? $"{nameParts[1]} {nameParts[2]}"
            : inspectorName;

        WelcomeText.Text = $"Добро пожаловать, {shortName}";

        UpdateClock();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();

        LoadLatestNews();

        Loaded += (s, e) =>
        {
            FindExpandableButtons();

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            fadeIn.Completed += (_, _) =>
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
                timer.Tick += (s2, e2) =>
                {
                    timer.Stop();
                    var fadeOutWelcome = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
                    var fadeInClock = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    fadeOutWelcome.Completed += (_, _) =>
                    {
                        ClockPanel.BeginAnimation(OpacityProperty, fadeInClock);
                        ClockSeparator.BeginAnimation(OpacityProperty, fadeInClock);
                    };
                    WelcomeText.BeginAnimation(OpacityProperty, fadeOutWelcome);
                };
                timer.Start();
            };
            WelcomeText.BeginAnimation(OpacityProperty, fadeIn);
        };
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockTime.Text = now.ToString("HH:mm");
        ClockDate.Text = now.ToString("d MMMM yyyy");
        ClockDayOfWeek.Text = now.ToString("dddd");
    }
    private void LoadLatestNews()
    {
        try
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "news.json");
            if (!File.Exists(path))
                path = System.IO.Path.Combine(@"C:\Users\evdok\BlankFiller\Data\", "news.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<NewsData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data?.News == null || data.News.Count == 0) return;

            var latest = data.News[^1];

            LatestNewsContent.Children.Clear();
            LatestNewsContent.Children.Add(new TextBlock
            {
                Text = latest.Title, FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["NewsAccent"]
            });
            LatestNewsContent.Children.Add(new TextBlock
            {
                Text = latest.Subtitle, FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 4, 0, 0)
            });
            LatestNewsContent.Children.Add(new TextBlock
            {
                Text = "Нажмите, чтобы узнать подробнее про обновление",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["PlaceholderColor"],
                Margin = new Thickness(0, 6, 0, 0)
            });

            LatestNewsBlock.Visibility = Visibility.Visible;
            GoToNewsButton.Visibility = Visibility.Visible;
        }
        catch { }
    }

    private void LatestNewsBlock_Click(object sender, MouseButtonEventArgs e)
    {
        var newsPage = new NewsPage();
        NavigateTo(newsPage);
        newsPage.ShowLatestModal();
    }

    private void GoToNewsButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new NewsPage());
    }

    // ==================== НАВИГАЦИЯ ====================

    public void NavigateTo(UserControl page)
    {
        HomeScreen.Visibility = Visibility.Collapsed;
        PageHost.Visibility = Visibility.Visible;
        PageHost.Content = page;
        StatusBarText.Text = "Заполнение формы...";
    }

    public void GoHome()
    {
        PageHost.Content = null;
        PageHost.Visibility = Visibility.Collapsed;
        HomeScreen.Visibility = Visibility.Visible;
        StatusBarText.Text = "Готов к работе";
    }

    private void GoHomeButton_Click(object sender, RoutedEventArgs e) => GoHome();

    // ==================== ОБРАБОТЧИКИ СТРАНИЦ ====================

    private void OpenActPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new ActPage(_inspectorTitleField, this));
    private void OpenNotificationPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new NotificationPage(_inspectorTitleField));
    private void OpenInvoicePage_Click(object sender, RoutedEventArgs e) => NavigateTo(new InvoicePage(_inspectorTitleField));
    private void OpenDataTransferPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new DataTransferPage(_inspectorName, _inspectorTitleField));
    private void OpenNewsPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new NewsPage());
    private void OpenRegistryPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new RegistryPageView(_inspectorName));
    private void OpenSettingsPage_Click(object sender, RoutedEventArgs e) => NavigateTo(new SettingsPage(_inspectorName));

    private void InDevelopment_Click(object sender, RoutedEventArgs e)
    {
        var rootGrid = (Grid)this.Content;

        var overlay = new Grid
        {
            Background = (Brush)Application.Current.Resources["OverlayBackground"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRowSpan(overlay, 100);

        var dialog = new Border
        {
            Background = (Brush)Application.Current.Resources["DialogBackground"],
            BorderBrush = (Brush)Application.Current.Resources["AccentColor"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(30, 24, 30, 24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 380
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "В разработке", FontSize = 18, FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["AccentColor"],
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Данная функция находится в разработке", FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 20)
        });

        var okBtn = new Button
        {
            Content = "OK", Width = 80,
            Background = (Brush)Application.Current.Resources["ButtonBackground"],
            Foreground = (Brush)Application.Current.Resources["AccentColor"],
            BorderBrush = (Brush)Application.Current.Resources["AccentColor"],
            BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6),
            Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stack.Children.Add(okBtn);
        dialog.Child = stack;
        overlay.Children.Add(dialog);

        Action close = () => rootGrid.Children.Remove(overlay);
        okBtn.Click += (s, e2) => close();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (s, e2) => { timer.Stop(); close(); };
        timer.Start();

        rootGrid.Children.Add(overlay);
    }

    // ==================== БОКОВАЯ ПАНЕЛЬ ====================

    private void FindExpandableButtons()
    {
        _expandableButtons.Clear();
        foreach (var btn in FindVisualChildren<Button>(Sidebar))
            if (btn.Tag is string tag && tag == "HasChildren") _expandableButtons.Add(btn);
    }

    private void CollapseAllSubmenus()
    {
        BlanksSubmenu.Visibility = Visibility.Collapsed;
        ProtocolSubmenu.Visibility = Visibility.Collapsed;
        DocsSubmenu.Visibility = Visibility.Collapsed;
        DatabaseSubmenu.Visibility = Visibility.Collapsed;
    }

    private void SetArrowsVisible(bool visible)
    {
        foreach (var btn in _expandableButtons)
        {
            var arrow = FindVisualChildByName<TextBlock>(btn, "Arrow");
            if (arrow != null) arrow.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name) return element;
            var result = FindVisualChildByName<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        if (_sidebarCollapsed)
        {
            CollapseAllSubmenus();
            SetArrowsVisible(false);
            Sidebar.Width = 56;
            ToggleSidebarButton.Content = "▶";
            SidebarLabel.Text = "";
            ToggleSidebarButton.ToolTip = new ToolTip
            {
                Content = "Развернуть боковую панель",
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xE0)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x26, 0xA6, 0x9A)),
                BorderThickness = new Thickness(1)
            };
        }
        else
        {
            SetArrowsVisible(true);
            Sidebar.Width = 240;
            ToggleSidebarButton.Content = "◀";
            SidebarLabel.Text = "Свернуть боковую панель";
            ToggleSidebarButton.ToolTip = null;
        }
    }

    private void ToggleBlanksSubmenu(object sender, RoutedEventArgs e)
    { if (!_sidebarCollapsed) BlanksSubmenu.Visibility = BlanksSubmenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

    private void ToggleProtocolSubmenu(object sender, RoutedEventArgs e)
    { if (!_sidebarCollapsed) ProtocolSubmenu.Visibility = ProtocolSubmenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

    private void ToggleDocsSubmenu(object sender, RoutedEventArgs e)
    { if (!_sidebarCollapsed) DocsSubmenu.Visibility = DocsSubmenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

    private void ToggleDatabaseSubmenu(object sender, RoutedEventArgs e)
    { if (!_sidebarCollapsed) DatabaseSubmenu.Visibility = DatabaseSubmenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

    // ==================== СМЕНА ПОЛЬЗОВАТЕЛЯ ====================

    private void SwitchUserButton_Click(object sender, RoutedEventArgs e) => ConfirmOverlay.Visibility = Visibility.Visible;
    private void CancelSwitch_Click(object sender, RoutedEventArgs e) => ConfirmOverlay.Visibility = Visibility.Collapsed;
    private void ConfirmSwitch_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new MainWindow();
        loginWindow.Show();
        this.Close();
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ ====================

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild) yield return tChild;
            foreach (var grandChild in FindVisualChildren<T>(child)) yield return grandChild;
        }
    }
}