using CyberList.Interfaces;

namespace CyberList.Tests;

/// <summary>Records calls instead of touching files. Folders in <see cref="FoldersWithDatabase"/> "contain" a database.</summary>
public sealed class FakeDatabaseService : IDatabaseService
{
    public string CurrentPath { get; set; } = Path.GetFullPath("default/test.db");
    public string DefaultPath { get; } = Path.GetFullPath("default/test.db");
    public bool IsUsingDefault => CurrentPath == DefaultPath;
    public string? StartupWarning { get; set; }
    public HashSet<string> FoldersWithDatabase { get; } = [];
    public List<string> Calls { get; } = [];

    /// <summary>Thrown by the next operation, to test error handling.</summary>
    public Exception? FailWith { get; set; }

    public event Action? DatabaseChanged;

    public bool TryRestoreConfiguredLocation() => false;
    public void ForgetConfiguredLocation() => StartupWarning = null;

    public string DatabasePathIn(string folder) => Path.GetFullPath(Path.Combine(folder, "test.db"));
    public bool HasDatabaseIn(string folder) => FoldersWithDatabase.Contains(folder);

    public Task MoveToAsync(string folder, bool overwrite) =>
        Run($"move {folder} overwrite={overwrite}", () => CurrentPath = DatabasePathIn(folder));

    public Task UseExistingAsync(string folder) =>
        Run($"use {folder}", () => CurrentPath = DatabasePathIn(folder));

    public Task ExportAsync(string filePath) => Run($"export {filePath}", () => { });

    public async Task<string> ImportAsync(string filePath)
    {
        await Run($"import {filePath}", () => { });
        return "backup.db";
    }

    private Task Run(string call, Action effect)
    {
        Calls.Add(call);
        if (FailWith is { } e)
            return Task.FromException(e);
        effect();
        DatabaseChanged?.Invoke();
        return Task.CompletedTask;
    }
}

/// <summary>Answers every picker with the next queued path (null = the user cancelled).</summary>
public sealed class FakeFileDialogService : IFileDialogService
{
    public Queue<string?> Answers { get; } = new();

    private Task<string?> Next() => Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);

    public Task<string?> PickFolderAsync(string title) => Next();
    public Task<string?> PickOpenFileAsync(string title) => Next();
    public Task<string?> PickSaveFileAsync(string title, string suggestedFileName) => Next();
}
