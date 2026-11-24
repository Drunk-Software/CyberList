using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Interfaces;
using Microsoft.Data.Sqlite;

namespace CyberList.ViewModels;

/// <summary>An inline question on the Settings page: a message and the buttons that answer it.</summary>
public sealed record ConfirmPrompt(string Message, IReadOnlyList<PromptChoice> Choices);

public sealed record PromptChoice(string Label, IAsyncRelayCommand Command);

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly IDatabaseService _database;
    private readonly IFileDialogService _dialogs;

    public SettingsPageViewModel(IDatabaseService database, IFileDialogService dialogs)
    {
        _database = database;
        _dialogs = dialogs;
        _database.DatabaseChanged += RefreshLocation;
    }

    // ===== Database location ===================================================

    public string DatabasePath => _database.CurrentPath;

    public bool IsDefaultLocation => _database.IsUsingDefault;

    public string LocationTag => IsDefaultLocation ? "DEFAULT" : "CUSTOM";

    /// <summary>The saved location was missing at startup. Shown as a banner with Retry / Use default.</summary>
    public string? StartupWarning => _database.StartupWarning;

    public bool HasStartupWarning => StartupWarning is not null;

    /// <summary>A question waiting for an answer (e.g. "a database already exists there").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrompt))]
    public partial ConfirmPrompt? Prompt { get; set; }

    public bool HasPrompt => Prompt is not null;

    /// <summary>Result of the last operation, shown under the sections.</summary>
    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeLocationCommand), nameof(ResetLocationCommand),
        nameof(ExportCommand), nameof(ImportCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanRun() => !IsBusy;

    private bool CanReset() => !IsBusy && !IsDefaultLocation;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ChangeLocationAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Choose the database folder");
        if (folder is not null)
            RequestMove(folder);
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void ResetLocation() => RequestMove(Path.GetDirectoryName(_database.DefaultPath)!);

    /// <summary>Move straight away, unless a database is already waiting in that folder: then ask.</summary>
    private void RequestMove(string folder)
    {
        var target = _database.DatabasePathIn(folder);
        if (string.Equals(target, _database.CurrentPath, StringComparison.OrdinalIgnoreCase))
        {
            ShowStatus("ALREADY USING THIS LOCATION.", isError: false);
            return;
        }

        if (!_database.HasDatabaseIn(folder))
        {
            _ = RunAsync(async () =>
            {
                await _database.MoveToAsync(folder, overwrite: false);
                return $"DATABASE MOVED TO {_database.CurrentPath}";
            });
            return;
        }

        Prompt = new ConfirmPrompt(
            $"A DATABASE ALREADY EXISTS AT {target}.",
            [
                new PromptChoice("USE EXISTING", new AsyncRelayCommand(() => RunAsync(async () =>
                {
                    await _database.UseExistingAsync(folder);
                    return $"NOW USING {_database.CurrentPath}";
                }))),
                new PromptChoice("REPLACE WITH CURRENT", new AsyncRelayCommand(() => RunAsync(async () =>
                {
                    await _database.MoveToAsync(folder, overwrite: true);
                    return $"DATABASE MOVED TO {_database.CurrentPath}. THE ONE IT REPLACED WAS BACKED UP.";
                }))),
            ]);
    }

    [RelayCommand]
    private void RetryLocation()
    {
        if (_database.TryRestoreConfiguredLocation())
            ShowStatus($"NOW USING {_database.CurrentPath}", isError: false);
        else
            ShowStatus("STILL NOT AVAILABLE.", isError: true);
    }

    [RelayCommand]
    private void KeepDefaultLocation()
    {
        _database.ForgetConfiguredLocation();
        RefreshLocation();
    }

    // ===== Import / export =====================================================

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportAsync()
    {
        var file = await _dialogs.PickSaveFileAsync(
            "Export database", $"cyberlist-{DateTime.Now:yyyy-MM-dd}.db");
        if (file is null)
            return;

        await RunAsync(async () =>
        {
            await _database.ExportAsync(file);
            return $"EXPORTED TO {file}";
        });
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ImportAsync()
    {
        var file = await _dialogs.PickOpenFileAsync("Import database");
        if (file is null)
            return;

        Prompt = new ConfirmPrompt(
            $"REPLACE ALL CURRENT DATA WITH {Path.GetFileName(file)}? THE CURRENT DATABASE IS BACKED UP FIRST.",
            [
                new PromptChoice("IMPORT", new AsyncRelayCommand(() => RunAsync(async () =>
                {
                    var backup = await _database.ImportAsync(file);
                    return $"IMPORTED. PREVIOUS DATA BACKED UP TO {backup}";
                }))),
            ]);
    }

    [RelayCommand]
    private void CancelPrompt() => Prompt = null;

    // ===== Helpers =============================================================

    /// <summary>Runs one operation with the buttons disabled, and turns its outcome into the status line.</summary>
    private async Task RunAsync(Func<Task<string>> operation)
    {
        Prompt = null;
        IsBusy = true;
        try
        {
            ShowStatus(await operation(), isError: false);
        }
        catch (InvalidDatabaseException e)
        {
            ShowStatus(e.Message, isError: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException)
        {
            ShowStatus($"FAILED: {e.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
            RefreshLocation();
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        Status = message;
        StatusIsError = isError;
    }

    private void RefreshLocation()
    {
        OnPropertyChanged(nameof(DatabasePath));
        OnPropertyChanged(nameof(IsDefaultLocation));
        OnPropertyChanged(nameof(LocationTag));
        OnPropertyChanged(nameof(StartupWarning));
        OnPropertyChanged(nameof(HasStartupWarning));
        ResetLocationCommand.NotifyCanExecuteChanged();
    }
}
