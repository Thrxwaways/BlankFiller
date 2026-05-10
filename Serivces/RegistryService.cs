using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BlankFiller.Models;

namespace BlankFiller.Services;

public class RegistryService
{
    private readonly string _filePath;
    private List<RegistryEntry> _entries = new();

    public IReadOnlyList<RegistryEntry> Entries => _entries;

    public RegistryService()
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "registry.json");
        Load();
    }

    public void Add(RegistryEntry entry)
    {
        _entries.Insert(0, entry);
        Save();
    }

    public void Remove(int index)
    {
        if (index >= 0 && index < _entries.Count)
        {
            _entries.RemoveAt(index);
            Save();
        }
    }

    public void UpdateReceiptNumber(int index, string number)
    {
        if (index >= 0 && index < _entries.Count)
        {
            _entries[index].ReceiptNumber = number;
            Save();
        }
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            _entries = JsonSerializer.Deserialize<List<RegistryEntry>>(json) ?? new();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}