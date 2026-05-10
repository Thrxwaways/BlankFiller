using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlankFiller.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlankFiller.Views.Pages;

public partial class SettingsPage : UserControl
{
    private readonly AuthService _auth;
    private readonly string _currentUserFullName;
    private Action? _confirmAction;
    private bool _initialized = false;
    private static bool _isLightTheme = false;

    public SettingsPage(string currentUserFullName)
    {
        InitializeComponent();
        _currentUserFullName = currentUserFullName;
        _auth = App.Host!.Services.GetRequiredService<AuthService>();

        CurrentUserText.Text = $"Текущий пользователь: {currentUserFullName}";

        ConfirmNoBtn.Click += (s, e) => ConfirmOverlay.Visibility = Visibility.Collapsed;
        ConfirmYesBtn.Click += (s, e) =>
        {
            ConfirmOverlay.Visibility = Visibility.Collapsed;
            _confirmAction?.Invoke();
        };

        ThemeToggle.IsChecked = _isLightTheme;
        _initialized = true;
    }

    private void ThemeToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        _isLightTheme = ThemeToggle.IsChecked == true;

        Application.Current.Resources.MergedDictionaries.Clear();

        if (_isLightTheme)
        {
            var lightDict = new ResourceDictionary { Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Add(lightDict);
        }
        else
        {
            var darkDict = new ResourceDictionary { Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Add(darkDict);
        }

        // Сохраняем выбор темы в файл
        SaveThemePreference();
    }

    private void SaveThemePreference()
    {
        try
        {
            var themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "theme.json");
            var json = JsonSerializer.Serialize(new { isLight = _isLightTheme });
            File.WriteAllText(themePath, json);
        }
        catch { }
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        OldPasswordBox.Password = "";
        NewPasswordBox.Password = "";
        ConfirmPasswordBox.Password = "";
        PasswordModalOverlay.Visibility = Visibility.Visible;
    }

    private void CancelPassword_Click(object sender, RoutedEventArgs e) => PasswordModalOverlay.Visibility = Visibility.Collapsed;

    private void SubmitPassword_Click(object sender, RoutedEventArgs e)
    {
        var oldPass = OldPasswordBox.Password;
        var newPass = NewPasswordBox.Password;
        var confirmPass = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
        { ShowNotification("Заполните все поля.", true); return; }
        if (newPass != confirmPass)
        { ShowNotification("Пароли не совпадают.", true); return; }
        if (!_auth.ValidatePassword(_currentUserFullName, oldPass))
        { ShowNotification("Введён неверный старый пароль.", true); return; }

        PasswordModalOverlay.Visibility = Visibility.Collapsed;
        ConfirmTitle.Text = "Смена пароля";
        ConfirmText.Text = "Вы действительно хотите сменить пароль?";
        _confirmAction = () => ExecutePasswordChange(newPass);
        ConfirmOverlay.Visibility = Visibility.Visible;
    }

    private void ExecutePasswordChange(string newPassword)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "users.json");
            if (!File.Exists(path))
                path = Path.Combine(@"C:\Users\evdok\BlankFiller\Data\", "users.json");

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<AuthData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
            {
                var user = data.Users.FirstOrDefault(u => u.Inspector == _currentUserFullName);
                if (user != null)
                {
                    user.Password = newPassword;
                    var updatedJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    File.WriteAllText(path, updatedJson);
                    ShowNotification("Пароль успешно изменён. Перезайдите в программу.", false, () => Application.Current.Shutdown());
                }
            }
        }
        catch (Exception ex) { ShowNotification($"Ошибка:\n{ex.Message}", true); }
    }

    private void ShowNotification(string msg, bool error = false, Action? onClose = null)
    {
        var color = error ? Color.FromRgb(0xFF, 0x8B, 0x5A) : Color.FromRgb(0x4C, 0xAF, 0x50);
        NotifTitle.Text = error ? "Ошибка" : "Готово";
        NotifTitle.Foreground = new SolidColorBrush(color);
        NotifText.Text = msg;
        NotificationOverlay.BorderBrush = new SolidColorBrush(color);
        NotificationOverlay.Visibility = Visibility.Visible;

        NotifOkBtn.Click -= NotifOkButton_Click;
        NotifOkBtn.Click -= NotifOk_WithAction;

        if (onClose != null)
        {
            RoutedEventHandler handler = null!;
            handler = (s, e) => { NotificationOverlay.Visibility = Visibility.Collapsed; NotifOkBtn.Click -= handler; onClose(); };
            NotifOkBtn.Click += handler;
        }
        else NotifOkBtn.Click += NotifOkButton_Click;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, ev) => { timer.Stop(); if (NotificationOverlay.Visibility == Visibility.Visible) { NotificationOverlay.Visibility = Visibility.Collapsed; onClose?.Invoke(); } };
        timer.Start();
    }

    private void NotifOkButton_Click(object sender, RoutedEventArgs e) => NotificationOverlay.Visibility = Visibility.Collapsed;
    private void NotifOk_WithAction(object sender, RoutedEventArgs e) => NotificationOverlay.Visibility = Visibility.Collapsed;
}