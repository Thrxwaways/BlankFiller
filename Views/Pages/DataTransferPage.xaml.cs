using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlankFiller.Models;
using Microsoft.Extensions.DependencyInjection;
using BlankFiller.Services;
using OfficeOpenXml;

namespace BlankFiller.Views.Pages;

public partial class DataTransferPage : UserControl
{
    private readonly FormDataService _formData;
    private readonly string _inspectorName;
    private bool _updating = false;
    private Action? _confirmAction;
    private bool _isDebtTransfer = false;

    public DataTransferPage(string inspectorName, string inspectorTitle)
    {
        InitializeComponent();
        _formData = App.Host!.Services.GetRequiredService<FormDataService>();
        _inspectorName = inspectorName;

        ExcelPackage.License.SetNonCommercialOrganization("CACTUS");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DebtDeliveryDateBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
        UnempDeliveryDateBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
        UnempShortNameBox.Text = GetShortName(inspectorName);

        LoadSharedData();
        _formData.DataChanged += OnFormDataChanged;

        DebtNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeName", DebtNameBox.Text); };
        DebtBirthBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthDate", DebtBirthBox.Text); };
        DebtResidenceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Residence", DebtResidenceBox.Text); };
        DebtReleaseDateBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ReleaseDate", DebtReleaseDateBox.Text); };
        DebtArticlePartBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ArticlePart", DebtArticlePartBox.Text); };
        DebtArticleBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Article", DebtArticleBox.Text); };
        DebtActNumberBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ActNumber", DebtActNumberBox.Text); };
        DebtDeliveredByBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DeliveredByUnit", DebtDeliveredByBox.Text); };

        UnempNameBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("DetaineeName", UnempNameBox.Text); };
        UnempBirthBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("BirthDate", UnempBirthBox.Text); };
        UnempResidenceBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Residence", UnempResidenceBox.Text); };
        UnempArticlePartBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("ArticlePart", UnempArticlePartBox.Text); };
        UnempArticleBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("Article", UnempArticleBox.Text); };
        UnempIdBox.TextChanged += (s, e) => { if (!_updating) _formData.Set("IdNumber", UnempIdBox.Text); };

        ConfirmNoBtn.Click += (s, e) =>
        {
            ConfirmOverlay.Visibility = Visibility.Collapsed;
            _confirmAction = null;
            _isDebtTransfer = false;
        };

        ConfirmYesBtn.Click += ConfirmYesBtn_DefaultClick;

        FullPaymentCheckBox.Checked += (s, e) => PartialPaymentCheckBox.IsChecked = false;
        PartialPaymentCheckBox.Checked += (s, e) => FullPaymentCheckBox.IsChecked = false;
    }

    private void ConfirmYesBtn_DefaultClick(object sender, RoutedEventArgs e)
    {
        ConfirmOverlay.Visibility = Visibility.Collapsed;
        _confirmAction?.Invoke();
        _confirmAction = null;

        if (_isDebtTransfer && PaymentCheckBox.IsChecked == true)
        {
            ExecuteRegistrySave();
        }
        _isDebtTransfer = false;
    }

    private string GetShortName(string fullName)
    {
        var parts = fullName.Split(' ');
        if (parts.Length >= 3) return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
        return fullName;
    }

    private void LoadSharedData()
    {
        _updating = true;
        LoadField(DebtNameBox, "DetaineeName"); LoadField(UnempNameBox, "DetaineeName");
        LoadField(DebtBirthBox, "BirthDate"); LoadField(UnempBirthBox, "BirthDate");
        LoadField(DebtResidenceBox, "Residence"); LoadField(UnempResidenceBox, "Residence");
        LoadField(DebtReleaseDateBox, "ReleaseDate");
        LoadField(DebtArticlePartBox, "ArticlePart"); LoadField(UnempArticlePartBox, "ArticlePart");
        LoadField(DebtArticleBox, "Article"); LoadField(UnempArticleBox, "Article");
        LoadField(DebtActNumberBox, "ActNumber");
        LoadField(DebtDeliveredByBox, "DeliveredByUnit", shorten: true);
        LoadField(UnempIdBox, "IdNumber");
        _updating = false;
    }

    private void LoadField(TextBox box, string key, bool shorten = false)
    {
        var value = _formData.Get(key);
        if (!string.IsNullOrEmpty(value))
        {
            if (shorten) value = ShortenDepartment(value);
            box.Text = value;
            box.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF));
        }
    }

    private void SetField(TextBox box, string value, bool shorten = false)
    {
        if (shorten) value = ShortenDepartment(value);
        box.Text = value;
        box.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF));
    }

    private void OnFormDataChanged(string key, string value)
    {
        Dispatcher.Invoke(() =>
        {
            _updating = true;
            switch (key)
            {
                case "DetaineeName": SetField(DebtNameBox, value); SetField(UnempNameBox, value); break;
                case "BirthDate": SetField(DebtBirthBox, value); SetField(UnempBirthBox, value); break;
                case "Residence": SetField(DebtResidenceBox, value); SetField(UnempResidenceBox, value); break;
                case "ReleaseDate": SetField(DebtReleaseDateBox, value); break;
                case "ArticlePart": SetField(DebtArticlePartBox, value); SetField(UnempArticlePartBox, value); break;
                case "Article": SetField(DebtArticleBox, value); SetField(UnempArticleBox, value); break;
                case "ActNumber": SetField(DebtActNumberBox, value); break;
                case "DeliveredByUnit": SetField(DebtDeliveredByBox, value, shorten: true); break;
                case "IdNumber": SetField(UnempIdBox, value); break;
            }
            _updating = false;
        });
    }

    private static string ShortenDepartment(string full)
    {
        if (string.IsNullOrWhiteSpace(full)) return full;
        var words = full.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = words[i] switch
            {
                "Заводского" => "Зав", "Ленинского" => "Лен", "Московского" => "Москв",
                "Партизанского" => "Парт", "Первомайского" => "Перв", "Советского" => "Сов",
                "Фрунзенского" => "Фрунз", "Центрального" => "Центр", _ => words[i]
            };
        }
        return string.Join(" ", words);
    }

    // ==================== ТАБЛИЦА ДОЛЖНИКОВ ====================

    private void TransferDebt_Click(object sender, RoutedEventArgs e)
    {
        _isDebtTransfer = true;
        ConfirmTitle.Text = "Перенос данных";
        ConfirmTitle.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmText.Inlines.Clear();
        ConfirmText.Inlines.Add(new Run("Вы уверены, что хотите перенести данные в"));
        ConfirmText.Inlines.Add(new Run(" таблицу должников") { Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7E, 0x57)), FontWeight = FontWeights.Bold });
        ConfirmText.Inlines.Add(new Run("?"));
        ConfirmText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));

        if (PaymentCheckBox.IsChecked == true)
        {
            ConfirmText.Inlines.Add(new Run("\n\nВАЖНО! Данные оплаты будут перенесены в раздел «Реестр»")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x45)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            });
        }

        ConfirmYesBtn.Content = "Да";
        ConfirmYesBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmYesBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x5A, 0x2D));
        ConfirmNoBtn.Visibility = Visibility.Visible;

        var parent = (Border)ConfirmOverlay.Child;
        parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

        _confirmAction = () => ExecuteDebtTransfer();
        ConfirmOverlay.Visibility = Visibility.Visible;
    }

    private void ExecuteDebtTransfer()
    {
        try
        {
            var filePath = ExcelFileHelper.FindFile(@"D:\", "списки должников");
            if (filePath == null) { ShowNotification("Файл не найден.", true); return; }

            CloseExcelFile(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var ws = package.Workbook.Worksheets["2026"];
            if (ws == null) { ShowNotification("Лист '2026' не найден.", true); return; }

            int newRow = 1;
            int lastRow = ws.Dimension?.End?.Row ?? 1;
            for (int r = lastRow; r >= 1; r--)
                if (!string.IsNullOrWhiteSpace(ws.Cells[r, 1].Text)) { newRow = r + 1; break; }

            ws.Cells[newRow, 1].Value = GetVal(DebtNameBox);
            ws.Cells[newRow, 2].Value = GetVal(DebtBirthBox);
            ws.Cells[newRow, 3].Value = GetVal(DebtResidenceBox);
            ws.Cells[newRow, 4].Value = GetVal(DebtDeliveryDateBox);
            ws.Cells[newRow, 5].Value = GetVal(DebtReleaseDateBox);
            ws.Cells[newRow, 6].Value = $"ч. {GetVal(DebtArticlePartBox)} ст. {GetVal(DebtArticleBox)}";
            ws.Cells[newRow, 7].Value = GetVal(DebtDeliveredByBox);
            ws.Cells[newRow, 8].Value = GetVal(DebtActNumberBox);

            if (PaymentCheckBox.IsChecked == true)
            {
                var sum = GetVal(PaymentSumBox, "Сумма");
                if (!string.IsNullOrWhiteSpace(sum)) ws.Cells[newRow, 9].Value = sum;
                ws.Cells[newRow, 10].Value = "при выписке";
                var method = ((ComboBoxItem)PaymentMethodCombo.SelectedItem)?.Content?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(method)) ws.Cells[newRow, 11].Value = method;

                System.Drawing.Color rowColor;
                if (FullPaymentCheckBox.IsChecked == true)
                    rowColor = System.Drawing.Color.FromArgb(255, 255, 255, 0);
                else if (PartialPaymentCheckBox.IsChecked == true)
                    rowColor = System.Drawing.Color.FromArgb(255, 146, 208, 80);
                else
                    rowColor = System.Drawing.Color.Empty;

                if (rowColor != System.Drawing.Color.Empty)
                    for (int col = 1; col <= 26; col++)
                    {
                        ws.Cells[newRow, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[newRow, col].Style.Fill.BackgroundColor.SetColor(rowColor);
                    }
            }

            package.Save();
            Dispatcher.BeginInvoke(new Action(() => ShowNotification("Данные успешно перенесены. Файл сохранён.")));
        }
        catch (Exception ex) { Dispatcher.BeginInvoke(new Action(() => ShowNotification($"Ошибка:\n{ex.Message}", true))); }
    }

    // ==================== ТАБЛИЦА НЕРАБОТАЮЩИХ ====================

    private void TransferUnemp_Click(object sender, RoutedEventArgs e)
    {
        _isDebtTransfer = false;
        ConfirmTitle.Text = "Перенос данных";
        ConfirmTitle.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmText.Inlines.Clear();
        ConfirmText.Inlines.Add(new Run("Вы уверены, что хотите перенести данные в"));
        ConfirmText.Inlines.Add(new Run(" таблицу неработающих") { Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0xC6, 0xDA)), FontWeight = FontWeights.Bold });
        ConfirmText.Inlines.Add(new Run("?"));
        ConfirmText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));

        ConfirmYesBtn.Content = "Да";
        ConfirmYesBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmYesBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x5A, 0x2D));
        ConfirmNoBtn.Visibility = Visibility.Visible;

        var parent = (Border)ConfirmOverlay.Child;
        parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

        _confirmAction = () => ExecuteUnempTransfer();
        ConfirmOverlay.Visibility = Visibility.Visible;
    }

    private void ExecuteUnempTransfer()
    {
        try
        {
            var filePath = ExcelFileHelper.FindFile(@"D:\", "Таблица по выдаче уведомлений СИ");
            if (filePath == null) { ShowNotification("Файл не найден.", true); return; }

            CloseExcelFile(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var ws = package.Workbook.Worksheets[0];
            if (ws == null) { ShowNotification("Лист не найден.", true); return; }

            int newRow = 1;
            int lastRow = ws.Dimension?.End?.Row ?? 1;
            for (int r = lastRow; r >= 1; r--)
                if (!string.IsNullOrWhiteSpace(ws.Cells[r, 2].Text)) { newRow = r + 1; break; }

            ws.Cells[newRow, 2].Value = GetVal(UnempNameBox);
            ws.Cells[newRow, 3].Value = GetVal(UnempBirthBox);
            ws.Cells[newRow, 4].Value = GetVal(UnempResidenceBox);
            ws.Cells[newRow, 5].Value = GetVal(UnempDeliveryDateBox);
            ws.Cells[newRow, 7].Value = GetVal(UnempShortNameBox);
            ws.Cells[newRow, 8].Value = $"ч. {GetVal(UnempArticlePartBox)} ст. {GetVal(UnempArticleBox)}";
            ws.Cells[newRow, 10].Value = GetVal(UnempIdBox);

            package.Save();
            Dispatcher.BeginInvoke(new Action(() => ShowNotification("Данные успешно перенесены. Файл сохранён.")));
        }
        catch (Exception ex) { Dispatcher.BeginInvoke(new Action(() => ShowNotification($"Ошибка:\n{ex.Message}", true))); }
    }

    // ==================== РЕЕСТР ====================

    private void SaveForRegistry_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentCheckBox.IsChecked != true)
        {
            ShowNotification("Нет данных для сохранения. Производилась оплата?", true);
            return;
        }

        // Проверка перед показом подтверждения
        var fullName = GetVal(DebtNameBox);
        var actNumber = GetVal(DebtActNumberBox);
        var paymentDate = GetVal(DebtDeliveryDateBox);
        var sum = GetVal(PaymentSumBox, "Сумма");
        var method = ((ComboBoxItem)PaymentMethodCombo.SelectedItem)?.Content?.ToString() ?? "";

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(fullName)) missingFields.Add("• Ф.И.О.");
        if (string.IsNullOrWhiteSpace(actNumber)) missingFields.Add("• Номер акта");
        if (string.IsNullOrWhiteSpace(paymentDate)) missingFields.Add("• Доставлен (дата оплаты)");
        if (string.IsNullOrWhiteSpace(sum)) missingFields.Add("• Сумма");
        if (string.IsNullOrWhiteSpace(method)) missingFields.Add("• Способ оплаты");

        if (missingFields.Count > 0)
        {
            ShowNotification("Не заполнены обязательные поля:\n" + string.Join("\n", missingFields), true);
            return;
        }

        ConfirmTitle.Text = "Сохранение в реестр";
        ConfirmTitle.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmText.Inlines.Clear();
        ConfirmText.Inlines.Add(new Run("Вы уверены, что хотите сохранить данные?"));
        ConfirmText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));

        ConfirmYesBtn.Content = "Да";
        ConfirmYesBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        ConfirmYesBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x5A, 0x2D));
        ConfirmNoBtn.Visibility = Visibility.Visible;

        var parent = (Border)ConfirmOverlay.Child;
        parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

        _confirmAction = () => ExecuteRegistrySave();
        ConfirmOverlay.Visibility = Visibility.Visible;
    }

    private void ExecuteRegistrySave()
    {
        var fullName = GetVal(DebtNameBox);
        var actNumber = GetVal(DebtActNumberBox);
        var paymentDate = GetVal(DebtDeliveryDateBox);
        var sum = GetVal(PaymentSumBox, "Сумма");
        var method = ((ComboBoxItem)PaymentMethodCombo.SelectedItem)?.Content?.ToString() ?? "";

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(fullName)) missingFields.Add("• Ф.И.О.");
        if (string.IsNullOrWhiteSpace(actNumber)) missingFields.Add("• Номер акта");
        if (string.IsNullOrWhiteSpace(paymentDate)) missingFields.Add("• Доставлен (дата оплаты)");
        if (string.IsNullOrWhiteSpace(sum)) missingFields.Add("• Сумма");
        if (string.IsNullOrWhiteSpace(method)) missingFields.Add("• Способ оплаты");

        if (missingFields.Count > 0)
        {
            ShowNotification("Не заполнены обязательные поля:\n" + string.Join("\n", missingFields), true);
            return;
        }

        var shortName = GetShortName(fullName);

        var entry = new RegistryEntry
        {
            FullName = fullName,
            ShortName = shortName,
            ActNumber = actNumber,
            Sum = sum,
            PaymentDate = paymentDate,
            PaymentMethod = method
        };

        var registry = App.Host!.Services.GetRequiredService<RegistryService>();
        registry.Add(entry);
        ShowNotification("Данные сохранены для реестра.");
    }

    private void GoToRegistry_Click(object sender, RoutedEventArgs e)
    {
        var parent = FindParent<MainMenuWindow>(this);
        parent?.NavigateTo(new RegistryPageView(_inspectorName));
    }

    // ==================== ОБЩИЕ МЕТОДЫ ====================

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

    private void ViewDebtTable_Click(object sender, RoutedEventArgs e) => OpenFile("списки должников");
    private void ViewUnempTable_Click(object sender, RoutedEventArgs e) => OpenFile("Таблица по выдаче уведомлений СИ");

    private void OpenFile(string pattern)
    {
        var overlay = MakeLoadingOverlay();
        AddOverlay(overlay);
        System.Threading.Tasks.Task.Run(() =>
        {
            var path = ExcelFileHelper.FindFile(@"D:\", pattern);
            Dispatcher.Invoke(() =>
            {
                RemoveOverlay(overlay);
                if (path == null) { ShowNotification($"Файл не найден: {pattern}", true); return; }
                try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); }
                catch (Exception ex) { ShowNotification($"Не удалось открыть:\n{ex.Message}", true); }
            });
        });
    }

    private Grid MakeLoadingOverlay()
    {
        var g = new Grid { Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)) };
        g.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        g.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "⏳", FontSize = 36, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) });
        sp.Children.Add(new TextBlock { Text = "Идёт загрузка...", FontSize = 16, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x26, 0xA6)), HorizontalAlignment = System.Windows.HorizontalAlignment.Center });

        var b = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x26, 0xA6)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16),
            Padding = new Thickness(40, 30, 40, 30),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Child = sp
        };

        g.Children.Add(b);
        return g;
    }

    private void ShowNotification(string msg, bool error = false)
    {
        ConfirmOverlay.Visibility = Visibility.Collapsed;

        var color = error ? Color.FromRgb(0xFF, 0x8B, 0x5A) : Color.FromRgb(0x4C, 0xAF, 0x50);
        var title = error ? "Ошибка" : "Готово";

        ConfirmTitle.Text = title;
        ConfirmTitle.Foreground = new SolidColorBrush(color);
        ConfirmText.Text = msg;
        ConfirmText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));

        ConfirmNoBtn.Visibility = Visibility.Collapsed;
        ConfirmYesBtn.Content = "OK";
        ConfirmYesBtn.Foreground = new SolidColorBrush(color);
        ConfirmYesBtn.BorderBrush = new SolidColorBrush(color);

        var parent = (Border)ConfirmOverlay.Child;
        parent.BorderBrush = new SolidColorBrush(color);

        ConfirmYesBtn.Click -= ConfirmYesBtn_DefaultClick;

        RoutedEventHandler closeHandler = null!;
        closeHandler = (s, e) =>
        {
            ConfirmOverlay.Visibility = Visibility.Collapsed;
            ConfirmNoBtn.Visibility = Visibility.Visible;
            ConfirmYesBtn.Content = "Да";
            ConfirmYesBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            ConfirmYesBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x5A, 0x2D));
            parent.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            ConfirmYesBtn.Click -= closeHandler;
            ConfirmYesBtn.Click += ConfirmYesBtn_DefaultClick;
        };

        ConfirmYesBtn.Click += closeHandler;
        ConfirmOverlay.Visibility = Visibility.Visible;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (s, e2) => { timer.Stop(); closeHandler(null!, null!); };
        timer.Start();
    }

    private void AddOverlay(Grid overlay)
    {
        var p = FindParent<MainMenuWindow>(this);
        ((Grid)((MainMenuWindow)p!).Content).Children.Add(overlay);
    }

    private void RemoveOverlay(Grid overlay)
    {
        var p = FindParent<MainMenuWindow>(this);
        var grid = (Grid)((MainMenuWindow)p!).Content;
        if (grid.Children.Contains(overlay))
            grid.Children.Remove(overlay);
    }

    private void PaymentCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = PaymentCheckBox.IsChecked == true;
        PaymentGrid.IsEnabled = enabled;
        PaymentGrid.Opacity = enabled ? 1 : 0.5;
        PaymentTypePanel.IsEnabled = enabled;
        PaymentTypePanel.Opacity = enabled ? 1 : 0.5;
    }

    private void Placeholder_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Tag is string p && box.Text == p)
        { box.Text = ""; box.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xFF)); }
    }

    private void Placeholder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Tag is string p && string.IsNullOrWhiteSpace(box.Text))
        { box.Text = p; box.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)); }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var p = VisualTreeHelper.GetParent(child);
        while (p != null) { if (p is T t) return t; p = VisualTreeHelper.GetParent(p); }
        return null;
    }
}