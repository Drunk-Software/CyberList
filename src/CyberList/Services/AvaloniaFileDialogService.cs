using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CyberList.Interfaces;

namespace CyberList.Services;

/// <summary>Opens the pickers over the main window, through its StorageProvider.</summary>
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType SqliteFiles = new("SQLite database")
    {
        Patterns = ["*.db", "*.sqlite", "*.sqlite3"],
    };

    private static IStorageProvider StorageProvider =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider
        ?? throw new InvalidOperationException("File dialogs need the main window to be open.");

    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickOpenFileAsync(string title)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [SqliteFiles, FilePickerFileTypes.All],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveFileAsync(string title, string suggestedFileName)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "db",
            FileTypeChoices = [SqliteFiles],
            ShowOverwritePrompt = true,
        });
        return file?.TryGetLocalPath();
    }
}
