using System.Collections.Generic;

namespace BlankFiller.Models;

public class NewsEntry
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public List<string> Items { get; set; } = new();
}

public class NewsData
{
    public List<NewsEntry> News { get; set; } = new();
}