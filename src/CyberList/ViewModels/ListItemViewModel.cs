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
        Children.CollectionChanged += (_, _) =>
        {
            foreach (var child in Children)
                child.RaiseShapeChanged();
            ChildStateChanged();
        };
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
    [NotifyPropertyChangedFor(nameof(ShowDownLine))]
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

    // --- Tree shape, for drawing the connector lines --------------------------------------
    // These are computed on read. When this item's Children change, RaiseShapeChanged tells
    // the rows below to re-read them (e.g. the old last child now needs a ├ instead of └).

    /// <summary>0 for roots, 1 for their children, and so on.</summary>
    public int Depth => Parent is null ? 0 : Parent.Depth + 1;

    /// <summary>True if no sibling comes after this item (its elbow line stops halfway).</summary>
    public bool IsLastChild => Parent is null || ReferenceEquals(Parent.Children.LastOrDefault(), this);

    /// <summary>
    /// For each indentation level to the left of this row, whether a vertical line passes
    /// through it: level L carries a line if the ancestor at depth L+1 still has siblings below.
    /// </summary>
    public IReadOnlyList<bool> GuideLines
    {
        get
        {
            var lines = new bool[Depth];
            for (var ancestor = Parent; ancestor is not null && ancestor.Depth >= 1; ancestor = ancestor.Parent)
                lines[ancestor.Depth - 1] = !ancestor.IsLastChild;
            return lines;
        }
    }

    /// <summary>An open folder draws a line from its node down to its first child.</summary>
    public bool ShowDownLine => IsExpanded && !IsLeaf;

    /// <summary>"done/total" leaves under a folder, e.g. "2/4".</summary>
    public string LeafSummary
    {
        get
        {
            var leaves = Descendants().Where(d => d.IsLeaf).ToList();
            return $"{leaves.Count(l => l.IsDone)}/{leaves.Count}";
        }
    }

    /// <summary>
    /// Something changed below: re-read the counter here and on every ancestor. Only ever
    /// raises notifications going UP, so it can't loop (same rule as ChildStateChanged).
    /// </summary>
    private void RaiseSubtreeChanged()
    {
        OnPropertyChanged(nameof(IsLeaf));
        OnPropertyChanged(nameof(LeafSummary));
        OnPropertyChanged(nameof(ShowDownLine));
        Parent?.RaiseSubtreeChanged();
    }

    /// <summary>
    /// A sibling was added or removed: this row's elbow and every descendant's pass-through
    /// lines may have changed. Goes DOWN the tree, and only re-reads shape, so it can't loop.
    /// </summary>
    private void RaiseShapeChanged()
    {
        OnPropertyChanged(nameof(IsLastChild));
        OnPropertyChanged(nameof(GuideLines));
        foreach (var child in Children)
            child.RaiseShapeChanged();
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
        RaiseSubtreeChanged();
        OnPropertyChanged(nameof(CheckState));
        IsDone = CheckState == true;
    }
}
