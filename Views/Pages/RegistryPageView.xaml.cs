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
using BlankFiller.Models;
using Microsoft.Extensions.DependencyInjection;
using BlankFiller.Services;
using Xceed.Words.NET;
using Border = System.Windows.Controls.Border;
using Orientation = System.Windows.Controls.Orientation;

namespace BlankFiller.Views.Pages;

public partial class RegistryPageView : UserControl
{
    private readonly RegistryService _registry;
    private readonly string _inspectorName;
    private Action? _confirmAction;

    public RegistryPageView(string inspectorName)
    {
        InitializeComponent();
        _inspectorName = inspectorName;
        _registry = App.Host!.Services.GetRequiredService<RegistryService>();

        // Восстановление данных из FormDataService
        var formData = App.Host!.Services.GetRequiredService<FormDataService>();
        if (!string.IsNullOrWhiteSpace(formData.Get("RegistryReceipt")))
            ReceiptNumberBox.Text = formData.Get("RegistryReceipt");
        if (!string.IsNullOrWhiteSpace(formData.Get("RegistryAct")))
            ActNumberBox.Text = formData.Get("RegistryAct");
        if (string.IsNullOrWhiteSpace(ComposeDateBox.Text))
            ComposeDateBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
        if (!string.IsNullOrWhiteSpace(formData.Get("RegistrySum")))
            SumBox.Text = formData.Get("RegistrySum");

        // Сохранение при изменении
        ReceiptNumberBox.TextChanged += (s, e) => formData.Set("RegistryReceipt", ReceiptNumberBox.Text);
        ActNumberBox.TextChanged += (s, e) => formData.Set("RegistryAct", ActNumberBox.Text);
        ComposeDateBox.TextChanged += (s, e) => formData.Set("RegistryDate", ComposeDateBox.Text);
        SumBox.TextChanged += (s, e) => formData.Set("RegistrySum", SumBox.Text);

        CashCheckBox.Checked += (s, e) => NonCashCheckBox.IsChecked = false;
        NonCashCheckBox.Checked += (s, e) => CashCheckBox.IsChecked = false;

        ConfirmNoBtn.Click += (s, e) => ConfirmOverlay.Visibility = Visibility.Collapsed;
        ConfirmYesBtn.Click += (s, e) =>
        {
            ConfirmOverlay.Visibility = Visibility.Collapsed;
            _confirmAction?.Invoke();
            _confirmAction = null;
        };

        LoadSavedEntries();
    }

    private void LoadSavedEntries()
    {
        SavedEntriesPanel.Children.Clear();

        for (int i = 0; i < _registry.Entries.Count; i++)
        {
            var entry = _registry.Entries[i];
            var index = i;

            var block = new Border
            {
                Background = (Brush)Application.Current.Resources["PanelBackground"],
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Width = 520
            };

            var mainStack = new StackPanel();

            mainStack.Children.Add(new TextBlock
            {
                Text = $"Реестр для {entry.ShortName}",
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var row1 = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            row1.Children.Add(CreateReadOnlyBlock("Номер акта:", entry.ActNumber, 80));
            row1.Children.Add(CreateReadOnlyBlock("Дата оплаты:", entry.PaymentDate, 90));
            mainStack.Children.Add(row1);

            var row2 = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            row2.Children.Add(CreateReadOnlyBlock("Сумма:", entry.Sum, 80));
            row2.Children.Add(CreateReadOnlyBlock("Способ:", entry.PaymentMethod, 70));
            mainStack.Children.Add(row2);

            var extraRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            extraRow.Children.Add(new TextBlock
            {
                Text = "Дополнить",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x45)),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            extraRow.Children.Add(new TextBlock
            {
                Text = "Номер квитанции:",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });

            var receiptBox = new TextBox
            {
                Text = entry.ReceiptNumber ?? "",
                Width = 80, FontSize = 11,
                Background = (Brush)Application.Current.Resources["InputBackground"],
                Foreground = (Brush)Application.Current.Resources["InputForeground"],
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x45)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            receiptBox.Template = (ControlTemplate)FindResource("RedRoundedTextBoxTemplate");
            receiptBox.TextChanged += (s, e) => _registry.UpdateReceiptNumber(index, receiptBox.Text);
            extraRow.Children.Add(receiptBox);
            mainStack.Children.Add(extraRow);

            var btnRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var deleteBtn = new Button { Content = "🗑 Удалить", Style = (Style)FindResource("DeleteButton") };
            var submitBtn = new Button { Content = "✓ Внести в реестр", Style = (Style)FindResource("GreenButton"), Margin = new Thickness(8, 0, 0, 0) };

            var idx = index;
            deleteBtn.Click += (s, e) => { _registry.Remove(idx); LoadSavedEntries(); };
            submitBtn.Click += (s, e) => SubmitEntry(idx);

            btnRow.Children.Add(deleteBtn);
            btnRow.Children.Add(submitBtn);
            mainStack.Children.Add(btnRow);

            block.Child = mainStack;
            SavedEntriesPanel.Children.Add(block);
        }
    }

    private Border CreateReadOnlyBlock(string label, string value, int width)
    {
        var b = new Border
        {
            Background = (Brush)Application.Current.Resources["PanelBackground"],
            BorderBrush = (Brush)Application.Current.Resources["AccentColor"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var sp = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = label + " ",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Width = width
        });

        b.Child = sp;
        return b;
    }

    private void SubmitEntry(int index)
    {
        var entry = _registry.Entries[index];
        if (string.IsNullOrWhiteSpace(entry.ReceiptNumber))
        { ShowNotification("Введите номер квитанции.", true); return; }
        ShowFileChoice(entry, index);
    }

    private void SubmitManualEntry_Click(object sender, RoutedEventArgs e)
    {
        var receipt = ReceiptNumberBox.Text.Trim();
        var act = ActNumberBox.Text.Trim();
        var sum = SumBox.Text.Trim();
        var date = ComposeDateBox.Text.Trim();
        var method = CashCheckBox.IsChecked == true ? "нал" : NonCashCheckBox.IsChecked == true ? "безнал" : "";

        if (string.IsNullOrWhiteSpace(receipt) || string.IsNullOrWhiteSpace(act) || string.IsNullOrWhiteSpace(sum) || string.IsNullOrWhiteSpace(method))
        { ShowNotification("Заполните все поля.", true); return; }

        ConfirmTitle.Text = "Внесение в реестр";
        ConfirmText.Text = "Вы уверены, что хотите внести данные в реестр?";
        _confirmAction = () =>
        {
            var entry = new RegistryEntry
            {
                FullName = _inspectorName, ShortName = GetShortName(_inspectorName),
                ActNumber = act, Sum = sum, PaymentDate = date,
                PaymentMethod = method, ReceiptNumber = receipt
            };
            ShowFileChoice(entry, -1);
        };
        ConfirmOverlay.Visibility = Visibility.Visible;
    }

    private string GetShortName(string fullName)
    {
        var parts = fullName.Split(' ');
        if (parts.Length >= 3) return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
        return fullName;
    }

    private void ShowFileChoice(RegistryEntry entry, int index)
    {
        var today = DateTime.Now;
        var patterns = new[] { today.AddDays(-1), today, today.AddDays(1) };
        var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Word", "Templates", "Reestr");
        if (!Directory.Exists(folder)) folder = Path.Combine(@"C:\Users\evdok\BlankFiller\Word\Templates\Reestr");

        var foundFiles = new List<string>();
        foreach (var date in patterns)
        {
            var pattern = $"Реестр {date:dd.MM.yyyy}";
            if (Directory.Exists(folder))
                foundFiles.AddRange(Directory.GetFiles(folder, "*.docx").Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(pattern)));
        }
        if (foundFiles.Count == 0) { ShowNotification("Файлы реестра не найдены.", true); return; }

        FileChoicePanel.Children.Clear();
        foreach (var file in foundFiles)
        {
            var btn = new Button
            {
                Content = Path.GetFileNameWithoutExtension(file),
                Background = (Brush)Application.Current.Resources["InputBackground"],
                Foreground = (Brush)Application.Current.Resources["InputForeground"],
                BorderBrush = (Brush)Application.Current.Resources["InputBorder"],
                BorderThickness = new Thickness(1), Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 3, 0, 3), Cursor = Cursors.Hand, FontSize = 13, Tag = file
            };
            btn.Click += (s, ev) => { FileChoiceOverlay.Visibility = Visibility.Collapsed; WriteToWord(file, entry, index); };
            FileChoicePanel.Children.Add(btn);
        }
        FileChoiceOverlay.Visibility = Visibility.Visible;
    }

    private void WriteToWord(string filePath, RegistryEntry entry, int index)
    {
        try
        {
            using var doc = DocX.Load(filePath);
            var table = doc.Tables.FirstOrDefault();
            if (table == null) { ShowNotification("Таблица не найдена.", true); return; }

            int newRow = table.RowCount;
            for (int r = 1; r < table.RowCount; r++)
                if (string.IsNullOrWhiteSpace(table.Rows[r].Cells[0].Paragraphs[0].Text)) { newRow = r; break; }
            if (newRow >= table.RowCount) table.InsertRow();

            var row = table.Rows[newRow];
            row.Cells[0].Paragraphs[0].Append(entry.ReceiptNumber ?? "");
            row.Cells[1].Paragraphs[0].Append(entry.ActNumber);
            row.Cells[2].Paragraphs[0].Append(entry.PaymentDate);
            row.Cells[3].Paragraphs[0].Append(entry.Sum);
            row.Cells[4].Paragraphs[0].Append(entry.PaymentMethod == "нал" ? "+" : "");
            row.Cells[5].Paragraphs[0].Append(entry.PaymentMethod == "безнал" ? "+" : "");
            doc.Save();

            if (index >= 0) { _registry.Remove(index); LoadSavedEntries(); }
            ShowNotification("Данные успешно внесены.");
        }
        catch (Exception ex) { ShowNotification($"Ошибка:\n{ex.Message}", true); }
    }

    private void ViewRegistry_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Now;
        var patterns = new[] { today.AddDays(-1), today, today.AddDays(1) };
        var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Word", "Templates", "Reestr");
        if (!Directory.Exists(folder)) folder = Path.Combine(@"C:\Users\evdok\BlankFiller\Word\Templates\Reestr");

        var foundFiles = new List<string>();
        foreach (var date in patterns)
        {
            var pattern = $"Реестр {date:dd.MM.yyyy}";
            if (Directory.Exists(folder))
                foundFiles.AddRange(Directory.GetFiles(folder, "*.docx").Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(pattern)));
        }
        if (foundFiles.Count == 0) { ShowNotification("Файлы не найдены.", true); return; }

        FileChoicePanel.Children.Clear();
        foreach (var file in foundFiles)
        {
            var btn = new Button
            {
                Content = Path.GetFileNameWithoutExtension(file),
                Background = (Brush)Application.Current.Resources["InputBackground"],
                Foreground = (Brush)Application.Current.Resources["InputForeground"],
                BorderBrush = (Brush)Application.Current.Resources["InputBorder"],
                BorderThickness = new Thickness(1), Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 3, 0, 3), Cursor = Cursors.Hand, FontSize = 13, Tag = file
            };
            btn.Click += (s, ev) =>
            {
                FileChoiceOverlay.Visibility = Visibility.Collapsed;
                try { Process.Start(new ProcessStartInfo { FileName = (string)btn.Tag, UseShellExecute = true }); }
                catch (Exception ex) { ShowNotification($"Не удалось открыть:\n{ex.Message}", true); }
            };
            FileChoicePanel.Children.Add(btn);
        }
        FileChoiceOverlay.Visibility = Visibility.Visible;
    }

    private void ShowNotification(string msg, bool error = false)
    {
        var color = error ? Color.FromRgb(0xFF, 0x8B, 0x5A) : Color.FromRgb(0x4C, 0xAF, 0x50);
        NotifTitle.Text = error ? "Ошибка" : "Готово";
        NotifTitle.Foreground = new SolidColorBrush(color);
        NotifText.Text = msg;
        NotificationOverlay.BorderBrush = new SolidColorBrush(color);
        NotificationOverlay.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (s, ev) => { timer.Stop(); NotificationOverlay.Visibility = Visibility.Collapsed; };
        timer.Start();
    }

    private void HideNotification_Click(object sender, RoutedEventArgs e) => NotificationOverlay.Visibility = Visibility.Collapsed;
}