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

public partial class NotificationPage : UserControl
{
    private readonly FormDataService _formData;
    private bool _updating = false;

    public NotificationPage(string inspectorTitle)
    {
        InitializeComponent();
        _formData = App.Host!.Services.GetRequiredService<FormDataService>();

        ExcelPackage.License.SetNonCommercialOrganization("CACTUS");

        InspectorBox.Text = inspectorTitle;
        ComposeDateBox.Text = DateTime.Now.ToString("dd.MM.yyyy");

        var auth = App.Host!.Services.GetRequiredService<AuthService>();
        IncomeSourceCombo.ItemsSource = auth.Data.IncomeSources;

        LoadSharedData();
        _formData.DataChanged += OnFormDataChanged;

        DetaineeNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeName", DetaineeNameBox.Text); };
        ResidenceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Residence", ResidenceBox.Text); };
        BirthDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthDate", BirthDateBox.Text); };
        BirthPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthPlace", BirthPlaceBox.Text); };
        ArticlePartBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ArticlePart", ArticlePartBox.Text); };
        ArticleBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Article", ArticleBox.Text); };
        LastWorkPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("LastWorkPlace", LastWorkPlaceBox.Text); };
        DismissalDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DismissalDate", DismissalDateBox.Text); };
        IdNumberBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("IdNumber", IdNumberBox.Text); };

        IncomeSourceCombo.AddHandler(TextBox.TextChangedEvent,
            new TextChangedEventHandler((s, e) => { if (!_updating) _formData.Set("IncomeSource", IncomeSourceCombo.Text); }));
    }

    private void LoadSharedData()
    {
        _updating = true;
        LoadField(DetaineeNameBox, "DetaineeName");
        LoadField(ResidenceBox, "Residence");
        LoadField(BirthDateBox, "BirthDate");
        LoadField(BirthPlaceBox, "BirthPlace");
        LoadField(IncomeSourceCombo, "IncomeSource");
        LoadField(ArticlePartBox, "ArticlePart");
        LoadField(ArticleBox, "Article");
        LoadField(LastWorkPlaceBox, "LastWorkPlace");
        LoadField(DismissalDateBox, "DismissalDate");
        LoadField(IdNumberBox, "IdNumber");
        _updating = false;
    }

    private void LoadField(TextBox box, string key)
    {
        var value = _formData.Get(key);
        if (!string.IsNullOrEmpty(value))
        {
            box.Text = value;
            var placeholder = box.Tag as string;
            box.Foreground = (placeholder != null && value == placeholder)
                ? new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77))
                : new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF));
        }
    }

    private void LoadField(ComboBox combo, string key)
    {
        var value = _formData.Get(key);
        if (!string.IsNullOrEmpty(value)) combo.Text = value;
    }

    private void SetField(TextBox box, string value)
    {
        box.Text = value;
        var placeholder = box.Tag as string;
        box.Foreground = (placeholder != null && value == placeholder)
            ? new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77))
            : new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF));
    }

    private void OnFormDataChanged(string key, string value)
    {
        Dispatcher.Invoke(() =>
        {
            _updating = true;
            switch (key)
            {
                case "DetaineeName": SetField(DetaineeNameBox, value); break;
                case "Residence": SetField(ResidenceBox, value); break;
                case "BirthDate": SetField(BirthDateBox, value); break;
                case "BirthPlace": SetField(BirthPlaceBox, value); break;
                case "IncomeSource": IncomeSourceCombo.Text = value; break;
                case "ArticlePart": SetField(ArticlePartBox, value); break;
                case "Article": SetField(ArticleBox, value); break;
                case "LastWorkPlace": SetField(LastWorkPlaceBox, value); break;
                case "DismissalDate": SetField(DismissalDateBox, value); break;
                case "IdNumber": SetField(IdNumberBox, value); break;
            }
            _updating = false;
        });
    }

    private static string GetVal(TextBox box, string? placeholder = null)
    {
        var t = box.Text;
        var p = placeholder ?? box.Tag as string;
        return (!string.IsNullOrEmpty(p) && t == p) ? "" : t;
    }

    private static int? CalculateAge(string? birthDate)
    {
        if (string.IsNullOrWhiteSpace(birthDate)) return null;

        // Формат ДД.ММ.ГГГГ
        var parts = birthDate.Split('.');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out var day) &&
            int.TryParse(parts[1], out var month) &&
            int.TryParse(parts[2], out var year))
        {
            try
            {
                var birth = new DateTime(year, month, day);
                var today = DateTime.Today;
                var age = today.Year - birth.Year;
                if (birth.Date > today.AddYears(-age)) age--;
                return age;
            }
            catch { return null; }
        }
        return null;
    }

    // ==================== ПЕРЕНОС В EXCEL ====================

    private void TransferToExcel_Click(object sender, RoutedEventArgs e)
    {
        ShowOverlay(
            "Перенос данных",
            "Вы уверены, что хотите перенести данные в бланк?",
            Color.FromRgb(0x4C, 0xAF, 0x50),
            () => ExecuteTransfer());
    }

    private void ExecuteTransfer()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excel", "Templates", "УведомлениеАнкета.xlsx");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(@"C:\Users\evdok\BlankFiller\Excel\Templates\", "УведомлениеАнкета.xlsx");
            }
            if (!File.Exists(filePath))
            {
                ShowOverlay("Ошибка", "Файл не найден.", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
                return;
            }

            CloseExcelFile(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var ws = package.Workbook.Worksheets["Данные"];
            if (ws == null)
            {
                ShowOverlay("Ошибка", "Лист 'Данные' не найден.", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
                return;
            }

            ws.Cells["B1"].Value = GetVal(ComposeDateBox);
            ws.Cells["B2"].Value = $"ч. {GetVal(ArticlePartBox, "Часть")} ст. {GetVal(ArticleBox, "Статья")}";
            ws.Cells["B3"].Value = GetVal(DetaineeNameBox);

            var age = CalculateAge(GetVal(BirthDateBox, "ДД.ММ.ГГГГ"));
            ws.Cells["B4"].Value = age?.ToString() ?? "";

            ws.Cells["B5"].Value = GetVal(BirthPlaceBox);
            ws.Cells["B6"].Value = GetVal(LastWorkPlaceBox);
            ws.Cells["B7"].Value = GetVal(DismissalDateBox, "Дата увольнения");
            ws.Cells["B8"].Value = IncomeSourceCombo.Text;
            ws.Cells["B9"].Value = GetVal(ResidenceBox);
            ws.Cells["B10"].Value = GetVal(PhoneBox, "нет");
            ws.Cells["B11"].Value = GetVal(IdNumberBox);
            ws.Cells["B12"].Value = InspectorBox.Text;
            ws.Cells["B13"].Value = _formData.Get("CurrentMedic");

            var auth = App.Host!.Services.GetRequiredService<AuthService>();
            var inspector = auth.GetInspector(_formData.Get("CurrentInspectorFullName"));
            if (inspector != null)
            {
                ws.Cells["C12"].Value = inspector.ShortName;
                ws.Cells["D12"].Value = inspector.Position;
                ws.Cells["E12"].Value = inspector.Rank;
            }

            package.Save();

            ShowOverlay(
                "Печать файла",
                "Вы хотите открыть бланк для печати?",
                Color.FromRgb(0xFF, 0x26, 0xA6),
                () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        ShowOverlay("Ошибка", $"Не удалось открыть:\n{ex.Message}", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
                    }
                },
                false);
        }
        catch (Exception ex)
        {
            ShowOverlay("Ошибка", $"Ошибка:\n{ex.Message}", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
        }
    }

    private static void CloseExcelFile(string filePath)
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("EXCEL"))
            {
                try
                {
                    if (proc.MainWindowTitle.Contains(Path.GetFileName(filePath)))
                    { proc.Kill(); proc.WaitForExit(3000); }
                }
                catch { }
            }
        }
        catch { }
    }

    // ==================== ОВЕРЛЕЙ ====================
    private void ShowOverlay(string title, string message, Color color, Action? onYes, bool errorOnly = false, bool showPrintHint = false)
    {
        var rootGrid = (Grid)(FindParent<MainMenuWindow>(this)?.Content ?? Application.Current.MainWindow.Content);

        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRowSpan(overlay, 100);

        var dialog = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x2A)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(30, 24, 30, 24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 380, MaxWidth = 500
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });
        stack.Children.Add(new TextBlock { Text = message, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20) });

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        if (errorOnly)
        {
            var btn = new Button { Content = "OK", Width = 100, Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x35)), Foreground = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            btn.Click += (_, _) => ((Grid)rootGrid).Children.Remove(overlay);
            btnPanel.Children.Add(btn);
        }
        else
        {
            var yes = new Button { Content = "Да", Width = 100, Background = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x15)), Foreground = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            var no = new Button { Content = "Нет", Width = 100, Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x35)), Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)), BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x5A)), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            yes.Click += (_, _) => { ((Grid)rootGrid).Children.Remove(overlay); onYes?.Invoke(); };
            no.Click += (_, _) => ((Grid)rootGrid).Children.Remove(overlay);
            btnPanel.Children.Add(yes);
            btnPanel.Children.Add(no);
        }
        stack.Children.Add(btnPanel);
        dialog.Child = stack;
        overlay.Children.Add(dialog);
        ((Grid)rootGrid).Children.Add(overlay);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    { var p = VisualTreeHelper.GetParent(child); while (p != null) { if (p is T t) return t; p = VisualTreeHelper.GetParent(p); } return null; }

    // ==================== ОСТАЛЬНОЕ ====================
    private void Placeholder_GotFocus(object sender, RoutedEventArgs e)
    { if (sender is TextBox box && box.Tag is string p && box.Text == p) { box.Text = ""; box.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF)); } }
    private void Placeholder_LostFocus(object sender, RoutedEventArgs e)
    { if (sender is TextBox box && box.Tag is string p && string.IsNullOrWhiteSpace(box.Text)) { box.Text = p; box.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)); } }
    private void ClearButton_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Visible;
    private void CancelClear_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Collapsed;
    private void ConfirmClear_Click(object sender, RoutedEventArgs e)
    {
        ConfirmClearOverlay.Visibility = Visibility.Collapsed;
        var inspector = InspectorBox.Text; var composeDate = ComposeDateBox.Text; var phone = PhoneBox.Text; var phoneTag = PhoneBox.Tag as string ?? "нет";
        var placeholders = new (TextBox Box, string P)[] { (ArticlePartBox, "Часть"), (ArticleBox, "Статья"), (DismissalDateBox, "Дата увольнения") };
        foreach (var tb in FindVisualChildren<TextBox>(this)) tb.Text = "";
        InspectorBox.Text = inspector; ComposeDateBox.Text = composeDate; IncomeSourceCombo.Text = "";
        PhoneBox.Text = phone == phoneTag ? phoneTag : "";
        var pc = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77));
        foreach (var (box, p) in placeholders) { box.Text = p; box.Foreground = pc; }
    }
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    { if (e.Key == Key.Tab && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { e.Handled = true; MoveToNextField(); } else if (e.Key == Key.Enter && Keyboard.FocusedElement is not ComboBox) { e.Handled = true; MoveToNextField(); } base.OnPreviewKeyDown(e); }
    private void MoveToNextField()
    { var all = FindVisualChildren<TextBox>(this).OfType<UIElement>().ToList(); var idx = -1; for (int i = 0; i < all.Count; i++) if (all[i].IsFocused) { idx = i; break; } if (idx >= 0 && idx < all.Count - 1) { all[idx + 1].Focus(); if (all[idx + 1] is TextBox tb) tb.SelectAll(); } else if (all.Count > 0) { all[0].Focus(); if (all[0] is TextBox tb2) tb2.SelectAll(); } }
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T tChild) yield return tChild; foreach (var grand in FindVisualChildren<T>(child)) yield return grand; } }
}