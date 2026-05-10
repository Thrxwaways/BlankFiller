using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlankFiller.Services;

public class InspectorEntry
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("rank")]
    public string Rank { get; set; } = string.Empty;
}

public class UserEntry
{
    [JsonPropertyName("inspector")]
    public string Inspector { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class AuthData
{
    [JsonPropertyName("appName")]
    public string AppName { get; set; } = "CACTUS";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "beta 0.1";

    [JsonPropertyName("inspectors")]
    public List<InspectorEntry> Inspectors { get; set; } = new();

    [JsonPropertyName("medics")]
    public List<string> Medics { get; set; } = new();

    [JsonPropertyName("users")]
    public List<UserEntry> Users { get; set; } = new();

    [JsonPropertyName("incomeSources")]
    public List<string> IncomeSources { get; set; } = new();
}

public class AuthService
{
    private readonly string _filePath;
    private AuthData _data;

    public AuthData Data => _data;

    public AuthService()
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "users.json");
        _data = LoadData();
    }

    private AuthData LoadData()
    {
        if (!File.Exists(_filePath))
            return new AuthData();

        try
        {
            var json = File.ReadAllText(_filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<AuthData>(json, options) ?? new AuthData();
        }
        catch
        {
            return new AuthData();
        }
    }

    public bool ValidatePassword(string fullName, string password)
    {
        var user = _data.Users.FirstOrDefault(u => u.Inspector == fullName);
        return user != null && user.Password == password;
    }

    public InspectorEntry? GetInspector(string fullName)
    {
        return _data.Inspectors.FirstOrDefault(i => i.FullName == fullName);
    }
}