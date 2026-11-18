using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Data.Entities;
using System.Collections.ObjectModel;

namespace CyberList.ViewModels;

public partial class ListItemViewModel : ViewModelBase
{
    public ListItemViewModel(ListItem entity, ListItemViewModel? parent)
    {
        Entity = entity;
        Parent = parent;
        Title = entity.Title;
        IsDone = entity.IsDone;
        EditTitle = entity.Title;
        Children.CollectionChanged += (_, _) => ChildStateChanged();
    }
    public ListItem Entity { get; }
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckState))]
    public partial bool IsDone { get; set; } 

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    public partial string EditTitle { get; set; }
    
    public ListItemViewModel? Parent { get; } 

    public ObservableCollection<ListItemViewModel> Children { get; } = [];

    /// <summary>A leaf has no children: it's a task, not a folder.</summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>Every node below this one, at any depth (not including this node).</summary>
    public IEnumerable<ListItemViewModel> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var grandchild in child.Descendants())
                yield return grandchild;
        }
    }

    /// <summary>Raised when the user toggles the checkbox, so the parent can persist it.</summary>
    public event Action<ListItemViewModel>? DoneChanged;
    public event Action<ListItemViewModel>? TitleChanged;
    public bool? CheckState
    {
        get
        {
            if (Children.Count == 0)
                return IsDone;
            if (Children.All(c => c.CheckState == true))
                return true;
            if (Children.All(c => c.CheckState == false))
                return false;
            return null;
        }
        set => SetDoneRecursive(value == true);
    }
    private void SetDoneRecursive(bool value)
    {
        foreach (var child in Children)
            child.SetDoneRecursive(value);
        IsDone = value;
    }
    partial void OnIsDoneChanged(bool value)
    {
        Entity.IsDone = value;
        DoneChanged?.Invoke(this);
        Parent?.ChildStateChanged();
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
        Title = EditTitle.Trim();
        IsEditing = false;
    }

    private bool CanSaveEdit() => !string.IsNullOrWhiteSpace(EditTitle) && EditTitle != Title;

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;
    private void ChildStateChanged()
    {
        OnPropertyChanged(nameof(CheckState));
        IsDone = CheckState == true;
    }
}
