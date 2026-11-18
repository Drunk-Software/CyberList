using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Data.Entities;

namespace CyberList.ViewModels;

public partial class ListItemViewModel(ListItem entity) : ViewModelBase
{
    public ListItem Entity { get; } = entity;
    [ObservableProperty]
    public partial string Title { get; set; } = entity.Title;

    [ObservableProperty]
    public partial bool IsDone { get; set; } = entity.IsDone;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    public partial string EditTitle { get; set; } = entity.Title;

    /// <summary>Raised when the user toggles the checkbox, so the parent can persist it.</summary>
    public event Action<ListItemViewModel>? DoneChanged;
    public event Action<ListItemViewModel>? TitleChanged;

    partial void OnIsDoneChanged(bool value)
    {
        Entity.IsDone = value;
        DoneChanged?.Invoke(this);
    }
    partial void OnTitleChanged(string value)
    {
        Entity.Title = value;
        TitleChanged?.Invoke(this);
    }
    [RelayCommand]
    private void BeginEdit()
    {
        EditTitle = Title;
        IsEditing = true;
    }
    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private void SaveEdit()
    {
        if (Title != EditTitle)
            return;
        Title = EditTitle.Trim();
        IsEditing = false;
    }

    private bool CanSaveEdit() => !string.IsNullOrWhiteSpace(EditTitle) && EditTitle != Title;

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;
}
