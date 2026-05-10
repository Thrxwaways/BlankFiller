using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BlankFiller.Services;

namespace BlankFiller.Views;

public partial class MainWindow : Window
{
    private readonly AuthService _auth;

    public MainWindow()
    {
        InitializeComponent();
        _auth = App.Host?.Services.GetRequiredService<AuthService>() ?? throw new InvalidOperationException("App host is not available");
        LoadData();
    }

    private void LoadData()
    {
        InspectorCombo.ItemsSource = _auth.Data.Inspectors;
        MedicCombo.ItemsSource = _auth.Data.Medics;

        if (InspectorCombo.Items.Count > 0)
            InspectorCombo.SelectedIndex = 0;

        if (MedicCombo.Items.Count > 0)
            MedicCombo.SelectedIndex = 0;

        PasswordTextBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
        PasswordPlaceholder.Visibility = Visibility.Visible;
        WarningBorder.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowWarning(string message)
    {
        WarningText.Text = message;
        WarningBorder.Visibility = Visibility.Visible;
        WarningBorder.Opacity = 1;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        ErrorBorder.Opacity = 1;
    }

    private void HideWarning_Click(object sender, RoutedEventArgs e)
    {
        WarningBorder.Visibility = Visibility.Collapsed;
    }

    private void HideError_Click(object sender, RoutedEventArgs e)
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Visibility == Visibility.Visible)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "🙈";
        }
        else
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "👁";
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void InspectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        WarningBorder.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void MedicCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        WarningBorder.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (InspectorCombo.SelectedItem is not InspectorEntry inspector)
        {
            ShowWarning("Выберите инспектора.");
            return;
        }

        if (MedicCombo.SelectedItem is not string medic || string.IsNullOrWhiteSpace(medic))
        {
            ShowWarning("Выберите фельдшера.");
            return;
        }

        var password = PasswordBox.Visibility == Visibility.Visible
            ? PasswordBox.Password
            : PasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowWarning("Введите пароль.");
            return;
        }

        if (!_auth.ValidatePassword(inspector.FullName, password))
        {
            ShowError("Неверный пароль для выбранного инспектора.");
            return;
        }

        var mainMenu = new MainMenuWindow(inspector.FullName, inspector.Title, medic);
        mainMenu.Show();
        Close();
    }
}
