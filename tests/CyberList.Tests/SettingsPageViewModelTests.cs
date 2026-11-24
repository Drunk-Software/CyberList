using CyberList.Interfaces;
using CyberList.ViewModels;

namespace CyberList.Tests;

public class SettingsPageViewModelTests
{
    private readonly FakeDatabaseService _db = new();
    private readonly FakeFileDialogService _dialogs = new();
    private readonly SettingsPageViewModel _page;

    public SettingsPageViewModelTests() => _page = new SettingsPageViewModel(_db, _dialogs);

    [Fact]
    public async Task Change_location_to_an_empty_folder_moves_without_asking()
    {
        _dialogs.Answers.Enqueue("usb");

        await _page.ChangeLocationCommand.ExecuteAsync(null);

        Assert.Equal(["move usb overwrite=False"], _db.Calls);
        Assert.False(_page.HasPrompt);
        Assert.Equal("CUSTOM", _page.LocationTag);
        Assert.False(_page.StatusIsError);
    }

    [Fact]
    public async Task Change_location_to_a_folder_with_a_database_asks_first()
    {
        _db.FoldersWithDatabase.Add("usb");
        _dialogs.Answers.Enqueue("usb");

        await _page.ChangeLocationCommand.ExecuteAsync(null);

        Assert.Empty(_db.Calls);
        Assert.Equal(["USE EXISTING", "REPLACE WITH CURRENT"], _page.Prompt!.Choices.Select(c => c.Label));

        await _page.Prompt.Choices[0].Command.ExecuteAsync(null);

        Assert.Equal(["use usb"], _db.Calls);
        Assert.False(_page.HasPrompt);
    }

    [Fact]
    public async Task Cancelling_the_picker_does_nothing()
    {
        await _page.ChangeLocationCommand.ExecuteAsync(null);

        Assert.Empty(_db.Calls);
        Assert.Null(_page.Status);
    }

    [Fact]
    public async Task Reset_is_only_available_on_a_custom_location()
    {
        Assert.False(_page.ResetLocationCommand.CanExecute(null));

        _dialogs.Answers.Enqueue("usb");
        await _page.ChangeLocationCommand.ExecuteAsync(null);

        Assert.True(_page.ResetLocationCommand.CanExecute(null));
    }

    [Fact]
    public async Task Import_asks_for_confirmation_before_replacing_data()
    {
        _dialogs.Answers.Enqueue("backup.db");

        await _page.ImportCommand.ExecuteAsync(null);
        Assert.Empty(_db.Calls);

        await _page.Prompt!.Choices.Single().Command.ExecuteAsync(null);
        Assert.Equal(["import backup.db"], _db.Calls);
    }

    [Fact]
    public async Task A_rejected_import_shows_the_reason_as_an_error()
    {
        _db.FailWith = new InvalidDatabaseException("NOT A SQLITE DATABASE.");
        _dialogs.Answers.Enqueue("junk.db");

        await _page.ImportCommand.ExecuteAsync(null);
        await _page.Prompt!.Choices.Single().Command.ExecuteAsync(null);

        Assert.True(_page.StatusIsError);
        Assert.Equal("NOT A SQLITE DATABASE.", _page.Status);
        Assert.False(_page.IsBusy);
    }

    [Fact]
    public void Startup_warning_is_shown_until_the_default_is_kept()
    {
        _db.StartupWarning = "MISSING";
        var page = new SettingsPageViewModel(_db, _dialogs);
        Assert.True(page.HasStartupWarning);

        page.KeepDefaultLocationCommand.Execute(null);

        Assert.False(page.HasStartupWarning);
    }
}
