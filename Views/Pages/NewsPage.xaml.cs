using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlankFiller.Models;

namespace BlankFiller.Views.Pages;

public partial class NewsPage : UserControl
{
    private List<NewsEntry> _news = new();

    public NewsPage()
    {
        InitializeComponent();
        LoadNews();
        BuildNewsList();
    }

    private void LoadNews()
    {
        var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "news.json");
        if (!File.Exists(path))
            path = System.IO.Path.Combine(@"C:\Users\evdok\BlankFiller\Data\", "news.json");
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<NewsData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (data?.News != null)
            _news = data.News;
    }

    private Brush GetResource(string key) => (Brush)Application.Current.Resources[key];

    private void BuildNewsList()
    {
        NewsListPanel.Children.Clear();
        var reversed = _news.AsEnumerable().Reverse().ToList();

        for (int i = 0; i < reversed.Count; i++)
        {
            var entry = reversed[i];
            var isLatest = (i == 0);

            var block = new Border
            {
                Background = GetResource("PanelBackground"),
                CornerRadius = new CornerRadius(12),
                BorderBrush = GetResource("NewsAccent"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20, 16, 20, 16),
                Margin = new Thickness(0, 0, 0, 14),
                Cursor = Cursors.Hand
            };

            var outerStack = new Grid();
            outerStack.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outerStack.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(new TextBlock
            {
                Text = entry.Title, FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = GetResource("NewsAccent")
            });
            stack.Children.Add(new TextBlock
            {
                Text = entry.Subtitle, FontSize = 14,
                Foreground = GetResource("TextPrimary"),
                Margin = new Thickness(0, 4, 0, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Нажмите, чтобы узнать подробнее",
                FontSize = 11,
                Foreground = GetResource("PlaceholderColor"),
                Margin = new Thickness(0, 6, 0, 0)
            });

            Grid.SetColumn(stack, 0);
            outerStack.Children.Add(stack);

            if (isLatest)
            {
                var badge = new Border
                {
                    Background = GetResource("NewsAccent"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 4, 10, 4),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = "Последние новости", FontSize = 10, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
                };
                Grid.SetColumn(badge, 1);
                outerStack.Children.Add(badge);
            }

            block.Child = outerStack;
            var entryCopy = entry;
            block.MouseLeftButtonDown += (s, e) => ShowModal(entryCopy);
            NewsListPanel.Children.Add(block);
        }
    }

    public void ShowLatestModal()
    {
        if (_news.Count > 0)
            ShowModal(_news[^1]);
    }

    private void ShowModal(NewsEntry entry)
    {
        ModalHeader.Children.Clear();
        ModalItemsPanel.Children.Clear();

        ModalHeader.Children.Add(new TextBlock { Text = entry.Title, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = GetResource("NewsAccent"), HorizontalAlignment = HorizontalAlignment.Center });
        ModalHeader.Children.Add(new TextBlock { Text = entry.Subtitle, FontSize = 16, Foreground = GetResource("TextPrimary"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
        ModalHeader.Children.Add(new Rectangle { Height = 1, Margin = new Thickness(0, 12, 0, 0), Fill = GetResource("InputBorder") });
        ModalHeader.Children.Add(new TextBlock { Text = "Список изменений", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = GetResource("AccentColor"), Margin = new Thickness(0, 10, 0, 0) });

        int index = 1;
        foreach (var item in entry.Items)
        {
            var itemBlock = new Border { Background = GetResource("InputBackground"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 3, 0, 0) };
            var row = new StackPanel { Orientation = Orientation.Horizontal };

            row.Children.Add(new Border
            {
                Background = GetResource("AccentColor"),
                BorderBrush = GetResource("AccentHover"),
                BorderThickness = new Thickness(1.5), CornerRadius = new CornerRadius(10),
                Width = 28, Height = 28, Margin = new Thickness(0, 0, 14, 0),
                Child = new TextBlock { Text = index.ToString(), Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x1A)), FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            });

            row.Children.Add(new TextBlock { Text = item.Replace("\\n", "\n"), FontSize = 13, Foreground = GetResource("TextPrimary"), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });

            itemBlock.Child = row;
            ModalItemsPanel.Children.Add(itemBlock);
            index++;
        }

        NewsModalOverlay.Visibility = Visibility.Visible;
    }

    private void CloseModal_Click(object sender, RoutedEventArgs e) => NewsModalOverlay.Visibility = Visibility.Collapsed;
}