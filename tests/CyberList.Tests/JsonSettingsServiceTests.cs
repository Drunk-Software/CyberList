using CyberList.Models;
using CyberList.Services;

namespace CyberList.Tests;

public sealed class JsonSettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CyberList.Tests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    public JsonSettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Missing_file_gives_defaults_and_creates_nothing()
    {
        var service = new JsonSettingsService(FilePath);

        Assert.Null(service.Current.Database.Path);
        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void Update_persists_across_instances()
    {
        new JsonSettingsService(FilePath).Update(s => s.Database.Path = @"D:\data\list.db");

        var reloaded = new JsonSettingsService(FilePath);

        Assert.Equal(@"D:\data\list.db", reloaded.Current.Database.Path);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Update_raises_SettingsChanged_after_saving()
    {
        var service = new JsonSettingsService(FilePath);
        AppSettings? raised = null;
        var fileExistedWhenRaised = false;
        service.SettingsChanged += s =>
        {
            raised = s;
            fileExistedWhenRaised = File.Exists(FilePath);
        };

        service.Update(s => s.Database.Path = "x.db");

        Assert.Same(service.Current, raised);
        Assert.True(fileExistedWhenRaised);
    }

    [Fact]
    public void Corrupt_file_gives_defaults_and_is_backed_up()
    {
        File.WriteAllText(FilePath, "{ not json");

        var service = new JsonSettingsService(FilePath);

        Assert.Null(service.Current.Database.Path);
        Assert.Equal("{ not json", File.ReadAllText(FilePath + ".bak"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{ "Database": null }""")]
    [InlineData("{}")]
    public void Null_or_missing_sections_fall_back_to_defaults(string json)
    {
        File.WriteAllText(FilePath, json);

        var service = new JsonSettingsService(FilePath);

        Assert.NotNull(service.Current.Database);
        Assert.Null(service.Current.Database.Path);
    }

    [Fact]
    public void Unknown_properties_from_newer_versions_are_ignored()
    {
        File.WriteAllText(FilePath, """{ "Database": { "Path": "a.db", "Future": 1 }, "Theme": "dark" }""");

        var service = new JsonSettingsService(FilePath);

        Assert.Equal("a.db", service.Current.Database.Path);
    }
}
