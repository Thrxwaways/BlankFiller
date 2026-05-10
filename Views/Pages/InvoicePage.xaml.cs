using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using BlankFiller.Services;
using OfficeOpenXml;

namespace BlankFiller.Views.Pages;

public partial class InvoicePage : UserControl
{
    private readonly FormDataService _formData;
    private bool _updating = false;
    private bool _conflictResolved = false;

    public InvoicePage(string inspectorTitle)
    {
        InitializeComponent();
        _formData = App.Host!.Services.GetRequiredService<FormDataService>();

        ExcelPackage.License.SetNonCommercialOrganization("CACTUS");

        InspectorBox.Text = inspectorTitle;
        LoadSharedData();
        _formData.DataChanged += OnFormDataChanged;

        DetaineeNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeName", DetaineeNameBox.Text); };
        BirthDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthDate", BirthDateBox.Text); };
        BirthPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthPlace", BirthPlaceBox.Text); };
        ResidenceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Residence", ResidenceBox.Text); };
        WorkPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("WorkPlace", WorkPlaceBox.Text); };
        PositionBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Position", PositionBox.Text); };
        PlacementTimeBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("PlacementTime", PlacementTimeBox.Text); };
        ActNumberBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ActNumber", ActNumberBox.Text); };
        ReleaseDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ReleaseDate", ReleaseDateBox.Text); };

        Loaded += (s, e) => ResolveWorkPlaceConflict();
    }

    private void LoadSharedData()
    {
        _updating = true;
        LoadField(DetaineeNameBox, "DetaineeName");
        LoadField(BirthDateBox, "BirthDate");
        LoadField(BirthPlaceBox, "BirthPlace");
        LoadField(ResidenceBox, "Residence");
        if (_conflictResolved)
        {
            LoadField(WorkPlaceBox, "WorkPlace");
            LoadField(PositionBox, "Position");
        }
        LoadField(PlacementTimeBox, "PlacementTime");
        LoadField(ActNumberBox, "ActNumber");
        LoadField(ReleaseDateBox, "ReleaseDate");
        _updating = false;
    }

    private void LoadField(TextBox box, string key)
    {
        var value = _formData.Get(key);
        if (!string.IsNullOrEmpty(value)) box.Text = value;
    }

    private void OnFormDataChanged(string key, string value)
    {
        Dispatcher.Invoke(() =>
        {
            _updating = true;
            switch (key)
            {
                case "DetaineeName": DetaineeNameBox.Text = value; break;
                case "BirthDate": BirthDateBox.Text = value; break;
                case "BirthPlace": BirthPlaceBox.Text = value; break;
                case "Residence": ResidenceBox.Text = value; break;
                case "WorkPlace": WorkPlaceBox.Text = value; break;
                case "Position": PositionBox.Text = value; break;
                case "PlacementTime": PlacementTimeBox.Text = value; break;
                case "ActNumber": ActNumberBox.Text = value; break;
                case "ReleaseDate": ReleaseDateBox.Text = value; break;
                case "IncomeSource":
                    _conflictResolved = false;
                    ResolveWorkPlaceConflict();
                    break;
            }
            _updating = false;
        });
    }

    private void ResolveWorkPlaceConflict()
    {
        if (_conflictResolved) return;

        var actWorkPlace = _formData.Get("WorkPlace");
        var actPosition = _formData.Get("Position");
        var notificationIncome = _formData.Get("IncomeSource");

        bool hasAct = !string.IsNullOrWhiteSpace(actWorkPlace);
        bool hasNotification = !string.IsNullOrWhiteSpace(notificationIncome);

        if (hasAct && hasNotification)
        {
            ShowWorkPlaceChoice(actWorkPlace, actPosition, notificationIncome);
        }
        else if (hasAct)
        {
            WorkPlaceBox.Text = actWorkPlace;
            PositionBox.Text = actPosition;
        }
        else if (hasNotification)
        {
            WorkPlaceBox.Text = "периодические подработки";
            PositionBox.Text = notificationIncome;
        }

        _conflictResolved = true;
    }

    private void ShowWorkPlaceChoice(string actWorkPlace, string actPosition, string notificationIncome)
    {
        Grid? rootGrid = null;

        var mainMenu = FindParent<MainMenuWindow>(this);
        if (mainMenu != null)
        {
            rootGrid = (Grid)mainMenu.Content;
        }
        else
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
                rootGrid = (Grid)mainWindow.Content;
        }

        if (rootGrid == null) return;

        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
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
            MinWidth = 460,
            MaxWidth = 520
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "Какое место работы нужно указать?",
            FontSize = 17, FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["AccentColor"],
            Margin = new Thickness(0, 0, 0, 16),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        var btn1 = CreateChoiceButton($"«{actWorkPlace}»\nДолжность: {actPosition}\n(из Акта помещения)", actWorkPlace, actPosition);
        btn1.Click += (s, e) =>
        {
            WorkPlaceBox.Text = actWorkPlace;
            PositionBox.Text = actPosition;
            _formData.Set("WorkPlace", actWorkPlace);
            _formData.Set("Position", actPosition);
            rootGrid.Children.Remove(overlay);
        };
        stack.Children.Add(btn1);

        var btn2 = CreateChoiceButton($"периодические подработки\nКем: {notificationIncome}\n(из Уведомления и анкеты)", "периодические подработки", notificationIncome);
        btn2.Click += (s, e) =>
        {
            WorkPlaceBox.Text = "периодические подработки";
            PositionBox.Text = notificationIncome;
            _formData.Set("WorkPlace", "периодические подработки");
            _formData.Set("Position", notificationIncome);
            rootGrid.Children.Remove(overlay);
        };
        stack.Children.Add(btn2);

        dialog.Child = stack;
        overlay.Children.Add(dialog);
        rootGrid.Children.Add(overlay);
    }

    private Button CreateChoiceButton(string text, string workPlaceValue, string positionValue)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["InputBackground"],
            BorderBrush = (Brush)Application.Current.Resources["InputBorder"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new TextBlock
            {
                Text = text,
                Foreground = (Brush)Application.Current.Resources["InputForeground"],
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            }
        };

        var btn = new Button
        {
            Content = border,
            Tag = (workPlaceValue, positionValue),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Padding = new Thickness(0)
        };

        return btn;
    }

    // ==================== ПЕРЕНОС В EXCEL ====================

    private void TransferToExcel_Click(object sender, RoutedEventArgs e)
    {
        ShowOverlay("Перенос данных", "Вы уверены, что хотите перенести данные в бланк?",
            Color.FromRgb(0x4C, 0xAF, 0x50), () => ExecuteTransfer());
    }

    private void ExecuteTransfer()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excel", "Templates", "Счёт.xlsx");
            if (!File.Exists(filePath))
                filePath = Path.Combine(@"C:\Users\evdok\BlankFiller\Excel\Templates\", "Счёт.xlsx");
            if (!File.Exists(filePath))
            { ShowOverlay("Ошибка", "Файл не найден.", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true); return; }

            CloseExcelFile(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var ws = package.Workbook.Worksheets["Данные"];
            if (ws == null) { ShowOverlay("Ошибка", "Лист 'Данные' не найден.", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true); return; }

            ws.Cells["B1"].Value = InspectorBox.Text;
            ws.Cells["B2"].Value = GetVal(DetaineeNameBox);
            ws.Cells["B3"].Value = GetVal(BirthDateBox, "ДД.ММ.ГГГГ");
            ws.Cells["B4"].Value = GetVal(BirthPlaceBox);
            ws.Cells["B5"].Value = GetVal(ResidenceBox);
            ws.Cells["B6"].Value = GetVal(WorkPlaceBox);
            ws.Cells["B7"].Value = GetVal(PositionBox);
            ws.Cells["B8"].Value = GetVal(PlacementTimeBox);
            ws.Cells["B9"].Value = GetVal(ActNumberBox);
            ws.Cells["B10"].Value = GetVal(ReleaseDateBox, "ДД.ММ.ГГГГ");

            var auth = App.Host!.Services.GetRequiredService<AuthService>();
            var inspector = auth.GetInspector(_formData.Get("CurrentInspectorFullName"));
            if (inspector != null)
            {
                ws.Cells["C1"].Value = inspector.ShortName;
                ws.Cells["D1"].Value = inspector.Position;
                ws.Cells["E1"].Value = inspector.Rank;
            }

            package.Save();

            ShowOverlay("Печать файла", "Вы хотите открыть бланк для печати?",
                Color.FromRgb(0xFF, 0x26, 0xA6),
                () =>
                {
                    try { Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true }); }
                    catch (Exception ex) { ShowOverlay("Ошибка", $"Не удалось открыть:\n{ex.Message}", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true); }
                }, false);
        }
        catch (Exception ex) { ShowOverlay("Ошибка", $"Ошибка:\n{ex.Message}", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true); }
    }

    private static void CloseExcelFile(string filePath)
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("EXCEL"))
                try { if (proc.MainWindowTitle.Contains(Path.GetFileName(filePath))) { proc.Kill(); proc.WaitForExit(3000); } } catch { }
        }
        catch { }
    }

    private static string GetVal(TextBox box, string? placeholder = null)
    {
        var t = box.Text;
        var p = placeholder ?? box.Tag as string;
        return (!string.IsNullOrEmpty(p) && t == p) ? "" : t;
    }

    private void ShowOverlay(string title, string message, Color color, Action? onYes, bool errorOnly = false)
    {
        var rootGrid = (Grid)(FindParent<MainMenuWindow>(this)?.Content ?? Application.Current.MainWindow.Content);
        if (rootGrid == null) return;

        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRowSpan(overlay, 100);

        var dialog = new Border
        {
            Background = (Brush)Application.Current.Resources["DialogBackground"],
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16),
            Padding = new Thickness(30, 24, 30, 24),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 380, MaxWidth = 500
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });
        stack.Children.Add(new TextBlock { Text = message, FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextSecondary"], HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20) });

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        if (errorOnly)
        {
            var btn = new Button { Content = "OK", Width = 100, Background = (Brush)Application.Current.Resources["ButtonBackground"], Foreground = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            btn.Click += (_, _) => rootGrid.Children.Remove(overlay);
            btnPanel.Children.Add(btn);
        }
        else
        {
            var yes = new Button { Content = "Да", Width = 100, Background = (Brush)Application.Current.Resources["GreenButtonBackground"], Foreground = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            var no = new Button { Content = "Нет", Width = 100, Background = (Brush)Application.Current.Resources["ButtonBackground"], Foreground = (Brush)Application.Current.Resources["TextSecondary"], BorderBrush = (Brush)Application.Current.Resources["ButtonBorder"], BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12 };
            yes.Click += (_, _) => { rootGrid.Children.Remove(overlay); onYes?.Invoke(); };
            no.Click += (_, _) => rootGrid.Children.Remove(overlay);
            btnPanel.Children.Add(yes); btnPanel.Children.Add(no);
        }
        stack.Children.Add(btnPanel);
        dialog.Child = stack;
        overlay.Children.Add(dialog);
        rootGrid.Children.Add(overlay);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    { var p = VisualTreeHelper.GetParent(child); while (p != null) { if (p is T t) return t; p = VisualTreeHelper.GetParent(p); } return null; }

    private void ClearButton_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Visible;
    private void CancelClear_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Collapsed;
    private void ConfirmClear_Click(object sender, RoutedEventArgs e)
    {
        ConfirmClearOverlay.Visibility = Visibility.Collapsed;
        var inspector = InspectorBox.Text;
        var actNumber = ActNumberBox.Text;
        var releaseDate = ReleaseDateBox.Text;
        foreach (var tb in FindVisualChildren<TextBox>(this)) tb.Text = "";
        InspectorBox.Text = inspector;
        ActNumberBox.Text = actNumber;
        ReleaseDateBox.Text = releaseDate;
    }
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    { if (e.Key == Key.Tab && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { e.Handled = true; MoveToNextField(); } else if (e.Key == Key.Enter && Keyboard.FocusedElement is not ComboBox) { e.Handled = true; MoveToNextField(); } base.OnPreviewKeyDown(e); }
    private void MoveToNextField()
    { var all = FindVisualChildren<TextBox>(this).OfType<UIElement>().ToList(); var idx = -1; for (int i = 0; i < all.Count; i++) if (all[i].IsFocused) { idx = i; break; } if (idx >= 0 && idx < all.Count - 1) { all[idx + 1].Focus(); if (all[idx + 1] is TextBox tb) tb.SelectAll(); } else if (all.Count > 0) { all[0].Focus(); if (all[0] is TextBox tb2) tb2.SelectAll(); } }
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T tChild) yield return tChild; foreach (var grand in FindVisualChildren<T>(child)) yield return grand; } }
}