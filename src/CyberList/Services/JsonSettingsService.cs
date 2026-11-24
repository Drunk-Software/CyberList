using System.Text.Json;
using CyberList.Interfaces;
using CyberList.Models;

namespace CyberList.Services;

/// <summary>Stores <see cref="AppSettings"/> as indented JSON in a single file.</summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly Lock _gate = new();

    public AppSettings Current { get; private set; }

    public event Action<AppSettings>? SettingsChanged;

    public JsonSettingsService(string filePath)
    {
        _filePath = filePath;
        Current = Load();
    }

    public void Update(Action<AppSettings> change)
    {
        lock (_gate)
        {
            change(Current);
            Normalize(Current);
            Save();
        }
        SettingsChanged?.Invoke(Current);
    }

    private AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return Normalize(settings);
        }
        catch (JsonException)
        {
            // Keep the broken file for inspection; the next Save writes a clean one.
            File.Copy(_filePath, _filePath + ".bak", overwrite: true);
            return new AppSettings();
        }
        catch (IOException)
        {
            // Locked or unreadable: run on defaults, but leave the file alone.
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Write to a temp file, then swap it in, so a crash mid-write can't corrupt the settings.</summary>
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(Current, JsonOptions));
        File.Move(tempPath, _filePath, overwrite: true);
    }

    /// <summary>A hand-edited file can null out whole sections (e.g. "Database": null).</summary>
    private static AppSettings Normalize(AppSettings settings)
    {
        settings.Database ??= new DatabaseSettings();
        return settings;
    }
}
