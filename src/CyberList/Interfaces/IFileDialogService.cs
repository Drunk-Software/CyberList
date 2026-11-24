namespace CyberList.Interfaces;

/// <summary>
/// Native file and folder pickers, so view models can ask for a path without touching Avalonia.
/// Every method returns null when the user cancels.
/// </summary>
public interface IFileDialogService
{
    Task<string?> PickFolderAsync(string title);

    Task<string?> PickOpenFileAsync(string title);

    /// <summary>The picker asks before overwriting an existing file.</summary>
    Task<string?> PickSaveFileAsync(string title, string suggestedFileName);
}
