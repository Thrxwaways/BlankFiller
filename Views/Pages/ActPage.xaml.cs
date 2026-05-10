using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using BlankFiller.Services;
using OfficeOpenXml;

namespace BlankFiller.Views.Pages;

public partial class ActPage : UserControl
{
    private readonly MainMenuWindow _mainMenu;
    private readonly FormDataService _formData;
    private readonly AuthService _auth;
    private bool _updating = false;

    public ActPage(string inspectorTitle, MainMenuWindow mainMenu)
    {
        InitializeComponent();
        _mainMenu = mainMenu;
        _formData = App.Host!.Services.GetRequiredService<FormDataService>();
        _auth = App.Host!.Services.GetRequiredService<AuthService>();
        ExcelPackage.License.SetNonCommercialOrganization("CACTUS");

        InspectorBox.Text = inspectorTitle;
        DeliveryDateBox.Text = DateTime.Now.ToString("dd.MM.yyyy");

        LoadSharedData();
        _formData.DataChanged += OnFormDataChanged;

        DeliveryTimeBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("PlacementTime", DeliveryTimeBox.Text); };
        ReleaseDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ReleaseDate", ReleaseDateBox.Text); };
        ActNumberBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ActNumber", ActNumberBox.Text); };
        DetaineeNumberBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeNumber", DetaineeNumberBox.Text); };
        ArticlePartBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ArticlePart", ArticlePartBox.Text); };
        ArticleBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Article", ArticleBox.Text); };
        DeliveredByUnitBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DeliveredByUnit", DeliveredByUnitBox.Text); };
        DeliveredByNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DeliveredByName", DeliveredByNameBox.Text); };

        DetaineeNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeName", DetaineeNameBox.Text); };
        BirthDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthDate", BirthDateBox.Text); };
        BirthPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthPlace", BirthPlaceBox.Text); };
        ResidenceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Residence", ResidenceBox.Text); };
        WorkPlaceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("WorkPlace", WorkPlaceBox.Text); };
        PositionBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Position", PositionBox.Text); };
        MaritalStatusBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("MaritalStatus", MaritalStatusBox.Text); };
        ChildrenBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Children", ChildrenBox.Text); };

        BelongingsLine1.TextChanged += (s, e) => { if (!_updating) _formData.Set("BelongingsLine1", BelongingsLine1.Text); };
        BelongingsLine2.TextChanged += (s, e) => { if (!_updating) _formData.Set("BelongingsLine2", BelongingsLine2.Text); };
        BelongingsLine3.TextChanged += (s, e) => { if (!_updating) _formData.Set("BelongingsLine3", BelongingsLine3.Text); };
        ShoesBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Shoes", ShoesBox.Text); };
        UnderwearBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Underwear", UnderwearBox.Text); };
        OuterwearBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Outerwear", OuterwearBox.Text); };
    }

    private string GetMedic()
    {
        return _formData.Get("CurrentMedic");
    }

    private void LoadSharedData()
    {
        _updating = true;
        LoadField(DeliveryTimeBox, "PlacementTime");
        LoadField(ReleaseDateBox, "ReleaseDate");
        LoadField(ActNumberBox, "ActNumber");
        LoadField(DetaineeNumberBox, "DetaineeNumber");
        LoadField(ArticlePartBox, "ArticlePart");
        LoadField(ArticleBox, "Article");
        LoadField(DeliveredByUnitBox, "DeliveredByUnit");
        LoadField(DeliveredByNameBox, "DeliveredByName");
        LoadField(DetaineeNameBox, "DetaineeName");
        LoadField(BirthDateBox, "BirthDate");
        LoadField(BirthPlaceBox, "BirthPlace");
        LoadField(ResidenceBox, "Residence");
        LoadField(WorkPlaceBox, "WorkPlace");
        LoadField(PositionBox, "Position");
        LoadField(MaritalStatusBox, "MaritalStatus");
        LoadField(ChildrenBox, "Children");
        LoadField(BelongingsLine1, "BelongingsLine1");
        LoadField(BelongingsLine2, "BelongingsLine2");
        LoadField(BelongingsLine3, "BelongingsLine3");
        LoadField(ShoesBox, "Shoes");
        LoadField(UnderwearBox, "Underwear");
        LoadField(OuterwearBox, "Outerwear");
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
                case "PlacementTime": SetField(DeliveryTimeBox, value); break;
                case "ReleaseDate": SetField(ReleaseDateBox, value); break;
                case "ActNumber": SetField(ActNumberBox, value); break;
                case "DetaineeNumber": SetField(DetaineeNumberBox, value); break;
                case "ArticlePart": SetField(ArticlePartBox, value); break;
                case "Article": SetField(ArticleBox, value); break;
                case "DeliveredByUnit": SetField(DeliveredByUnitBox, value); break;
                case "DeliveredByName": SetField(DeliveredByNameBox, value); break;
                case "DetaineeName": SetField(DetaineeNameBox, value); break;
                case "BirthDate": SetField(BirthDateBox, value); break;
                case "BirthPlace": SetField(BirthPlaceBox, value); break;
                case "Residence": SetField(ResidenceBox, value); break;
                case "WorkPlace": SetField(WorkPlaceBox, value); break;
                case "Position": SetField(PositionBox, value); break;
                case "MaritalStatus": SetField(MaritalStatusBox, value); break;
                case "Children": SetField(ChildrenBox, value); break;
                case "BelongingsLine1": SetField(BelongingsLine1, value); break;
                case "BelongingsLine2": SetField(BelongingsLine2, value); break;
                case "BelongingsLine3": SetField(BelongingsLine3, value); break;
                case "Shoes": SetField(ShoesBox, value); break;
                case "Underwear": SetField(UnderwearBox, value); break;
                case "Outerwear": SetField(OuterwearBox, value); break;
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

    // ==================== ПЕРЕНОС В EXCEL ====================

    private void TransferToExcel_Click(object sender, RoutedEventArgs e)
    {
        ShowOverlay(
            "Перенос данных",
            "Вы уверены, что хотите перенести данные в таблицу?",
            Color.FromRgb(0x4C, 0xAF, 0x50),
            () => ExecuteTransfer());
    }

    private void ExecuteTransfer()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excel", "Templates", "АктПомещения.xlsx");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(@"C:\Users\evdok\BlankFiller\Excel\Templates\", "АктПомещения.xlsx");
            }
            if (!File.Exists(filePath))
            {
                ShowOverlay("Ошибка", "Файл АктПомещения.xlsx не найден.", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
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

            ws.Cells["B1"].Value = GetVal(DeliveryDateBox);
            ws.Cells["B2"].Value = GetVal(DeliveryTimeBox, "ЧЧ:ММ");
            ws.Cells["B3"].Value = GetVal(ReleaseDateBox, "ДД.ММ.ГГГГ");
            ws.Cells["B4"].Value = GetVal(ActNumberBox);
            ws.Cells["B5"].Value = GetVal(DetaineeNumberBox);
            ws.Cells["B6"].Value = $"ч. {GetVal(ArticlePartBox, "Часть")} ст. {GetVal(ArticleBox, "Статья")}";
            ws.Cells["B7"].Value = $"{GetVal(DeliveredByUnitBox, "Подразделение")} {GetVal(DeliveredByNameBox, "Фамилия, инициалы сотрудника")}".Trim();
            ws.Cells["B8"].Value = GetVal(DetaineeNameBox);
            ws.Cells["B9"].Value = GetVal(BirthDateBox, "ДД.ММ.ГГГГ");
            ws.Cells["B10"].Value = GetVal(BirthPlaceBox);
            ws.Cells["B11"].Value = GetVal(ResidenceBox);
            ws.Cells["B12"].Value = GetVal(WorkPlaceBox);
            ws.Cells["B13"].Value = GetVal(PositionBox);
            ws.Cells["B14"].Value = GetVal(MaritalStatusBox);
            ws.Cells["B15"].Value = GetVal(ChildrenBox);
            ws.Cells["B16"].Value = GetVal(BelongingsLine1, "Строка 1");
            ws.Cells["B17"].Value = GetVal(BelongingsLine2, "Строка 2");
            ws.Cells["B18"].Value = GetVal(BelongingsLine3, "Строка 3");
            ws.Cells["B19"].Value = $"{GetVal(ShoesBox, "Цвет обуви")} обувь".Trim();
            ws.Cells["B20"].Value = $"{GetVal(UnderwearBox, "Цвет нижней одежды")} нижняя одежда".Trim();
            ws.Cells["B21"].Value = $"{GetVal(OuterwearBox, "Цвет верхней одежды")} верхняя одежда".Trim();
            ws.Cells["B22"].Value = InspectorBox.Text;
            ws.Cells["B23"].Value = GetMedic();

            // Данные инспектора из JSON
            var auth = App.Host!.Services.GetRequiredService<AuthService>();
            var inspector = auth.GetInspector(_formData.Get("CurrentInspectorFullName"));

            if (inspector != null)
            {
                ws.Cells["C22"].Value = inspector.ShortName;
                ws.Cells["D22"].Value = inspector.Position;
                ws.Cells["E22"].Value = inspector.Rank;
            }

            package.Save();

            // Окно открытия файла
            ShowOverlay(
                "Печать файла",
                "Вы хотите открыть бланк для печати?",
                Color.FromRgb(0xFF, 0x26, 0xA6),
                () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        ShowOverlay("Ошибка", $"Не удалось открыть файл:\n{ex.Message}",
                            Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
                    }
                },
                false,
                showPrintHint: false);
        }
        catch (Exception ex)
        {
            ShowOverlay("Ошибка", $"Ошибка переноса:\n{ex.Message}", Color.FromRgb(0xFF, 0x8B, 0x5A), null, true);
        }
    }

    private static string GetPositionBeforeMinsk(string title)
    {
        var index = title.IndexOf("г. Минска");
        if (index > 0)
            return title.Substring(0, index + "г. Минска".Length).Trim();
        return title;
    }

    private static string GetRankAfterMinsk(string title)
    {
        var index = title.IndexOf("г. Минска");
        if (index < 0) return "";

        var after = title.Substring(index + "г. Минска".Length).Trim();
        var words = after.Split(' ').ToList();
        if (words.Count >= 3)
            words.RemoveRange(words.Count - 3, 3);

        return string.Join(" ", words).Trim();
    }

    private static string GetShortName(string fullName)
    {
        var parts = fullName.Split(' ');
        if (parts.Length >= 3)
            return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
        return fullName;
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
        var rootGrid = (Grid)_mainMenu.Content;

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
            MinWidth = 380,
            MaxWidth = 500
        };

        var stack = new StackPanel();

        stack.Children.Add(new TextBlock
        {
            Text = title, FontSize = 18, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
        });

        stack.Children.Add(new TextBlock
        {
            Text = message, FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        });

        if (showPrintHint)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "ВАЖНО",
                FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x45)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Сначала печатаем страницы от 1 до 1, затем от 2 до 2",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            });
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

        if (errorOnly)
        {
            var okBtn = new Button
            {
                Content = "OK", Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x35)),
                Foreground = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand,
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okBtn.Click += (_, _) => rootGrid.Children.Remove(overlay);
            btnPanel.Children.Add(okBtn);
        }
        else
        {
            var yesBtn = new Button
            {
                Content = "Да", Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x15)),
                Foreground = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand,
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var noBtn = new Button
            {
                Content = "Нет", Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x5A)), BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6), Cursor = Cursors.Hand,
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            yesBtn.Click += (_, _) =>
            {
                rootGrid.Children.Remove(overlay);
                onYes?.Invoke();
            };
            noBtn.Click += (_, _) => rootGrid.Children.Remove(overlay);

            btnPanel.Children.Add(yesBtn);
            btnPanel.Children.Add(noBtn);
        }

        stack.Children.Add(btnPanel);
        dialog.Child = stack;
        overlay.Children.Add(dialog);
        rootGrid.Children.Add(overlay);
    }

    // ==================== ОСТАЛЬНОЕ ====================

    private void Placeholder_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Tag is string placeholder && box.Text == placeholder)
        { box.Text = ""; box.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF)); }
    }

    private void Placeholder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Tag is string placeholder && string.IsNullOrWhiteSpace(box.Text))
        { box.Text = placeholder; box.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)); }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Visible;
    private void CancelClear_Click(object sender, RoutedEventArgs e) => ConfirmClearOverlay.Visibility = Visibility.Collapsed;

    private void ConfirmClear_Click(object sender, RoutedEventArgs e)
    {
        ConfirmClearOverlay.Visibility = Visibility.Collapsed;
        var inspector = InspectorBox.Text;
        var deliveryDate = DeliveryDateBox.Text;
        var actNumber = ActNumberBox.Text;
        var detaineeNumber = DetaineeNumberBox.Text;
        var releaseDate = ReleaseDateBox.Text;

        var placeholders = new (TextBox Box, string P)[]
        {
            (BelongingsLine1, BelongingsLine1.Tag as string ?? "Строка 1"),
            (BelongingsLine2, BelongingsLine2.Tag as string ?? "Строка 2"),
            (BelongingsLine3, BelongingsLine3.Tag as string ?? "Строка 3"),
            (ShoesBox, ShoesBox.Tag as string ?? "Цвет обуви"),
            (UnderwearBox, UnderwearBox.Tag as string ?? "Цвет нижней одежды"),
            (OuterwearBox, OuterwearBox.Tag as string ?? "Цвет верхней одежды"),
            (ArticlePartBox, ArticlePartBox.Tag as string ?? "Часть"),
            (ArticleBox, ArticleBox.Tag as string ?? "Статья"),
            (DeliveredByUnitBox, DeliveredByUnitBox.Tag as string ?? "Подразделение"),
            (DeliveredByNameBox, DeliveredByNameBox.Tag as string ?? "Фамилия, инициалы сотрудника"),
        };

        foreach (var tb in FindVisualChildren<TextBox>(this)) tb.Text = "";
        InspectorBox.Text = inspector;
        DeliveryDateBox.Text = deliveryDate;
        ActNumberBox.Text = actNumber;
        DetaineeNumberBox.Text = detaineeNumber;
        ReleaseDateBox.Text = releaseDate;

        var pc = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77));
        foreach (var (box, p) in placeholders) { box.Text = p; box.Foreground = pc; }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Tab && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { e.Handled = true; MoveToNextField(); }
        else if (e.Key == Key.Enter && Keyboard.FocusedElement is not ComboBox) { e.Handled = true; MoveToNextField(); }
        base.OnPreviewKeyDown(e);
    }

    private void MoveToNextField()
    {
        var all = FindVisualChildren<TextBox>(this).OfType<UIElement>().ToList();
        var idx = -1;
        for (int i = 0; i < all.Count; i++) if (all[i].IsFocused) { idx = i; break; }
        if (idx >= 0 && idx < all.Count - 1) { all[idx + 1].Focus(); if (all[idx + 1] is TextBox tb) tb.SelectAll(); }
        else if (all.Count > 0) { all[0].Focus(); if (all[0] is TextBox tb2) tb2.SelectAll(); }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        { var child = VisualTreeHelper.GetChild(parent, i); if (child is T tChild) yield return tChild; foreach (var grand in FindVisualChildren<T>(child)) yield return grand; }
    }
}