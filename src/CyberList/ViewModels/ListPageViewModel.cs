using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Data;
using CyberList.Data.Entities;
using CyberList.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CyberList.ViewModels;

public partial class ListPageViewModel : ViewModelBase, IHasTabs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public IReadOnlyList<PageTab> Tabs { get; }
    public ListPageViewModel(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Tabs =
        [
            new PageTab("01 ALL",    () => Filter = ItemFilter.All),
            new PageTab("02 ACTIVE", () => Filter = ItemFilter.Active),
            new PageTab("03 DONE",   () => Filter = ItemFilter.Done),
        ];
        Tabs[1].IsSelected = true;
        RootItems.CollectionChanged += (_, _) => RefreshProperties();
        _ = LoadAsync();
    }
    public ObservableCollection<ListItemViewModel> RootItems { get; } = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewTitle { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ItemFilter Filter { get; set; } = ItemFilter.Active;

    partial void OnFilterChanged(ItemFilter value) => UpdateVisibleItems();

    /// <summary>Nothing to show under the current filter: the view shows a hint instead.</summary>
    public bool IsEmpty => VisibleItems.Count == 0;

    /// <summary>
    /// The root rows the tree shows. A snapshot that is only replaced when the set of visible
    /// roots actually changes: replacing it rebuilds every row, which would lose the selection
    /// and keyboard focus (and the E / A / Del shortcuts with them) on every checkbox click.
    /// </summary>
    public IReadOnlyList<ListItemViewModel> VisibleItems { get; private set; } = [];

    private void UpdateVisibleItems()
    {
        var visible = Filter switch
        {
            ItemFilter.Active => RootItems.Where(i => !i.IsDone).ToList(),
            ItemFilter.Done => RootItems.Where(i => i.IsDone).ToList(),
            _ => RootItems.ToList()
        };
        if (visible.SequenceEqual(VisibleItems))
            return;

        VisibleItems = visible;
        OnPropertyChanged(nameof(VisibleItems));
        OnPropertyChanged(nameof(IsEmpty));
    }
    /// <summary>Every leaf in the whole tree. Leaves are the actual tasks; parents are folders.</summary>
    private IEnumerable<ListItemViewModel> Leaves =>
        RootItems
            .SelectMany(root => root.Descendants().Prepend(root))
            .Where(i => i.IsLeaf);

    public string Summary
    {
        get
        {
            var leaves = Leaves.ToList();
            return $"{leaves.Count(i => i.IsDone)} of {leaves.Count} items";
        }
    }

    /// <summary>Fraction of leaves done, 0..1. Feeds the SegmentBar.</summary>
    public double Progress
    {
        get
        {
            var leaves = Leaves.ToList();
            return leaves.Count == 0 ? 0 : (double)leaves.Count(i => i.IsDone) / leaves.Count;
        }
    }

    /// <summary>Number of tasks (leaves) in the whole tree. Feeds the ENT corner label.</summary>
    public int LeafCount => Leaves.Count();
    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.Items.AsNoTracking().OrderBy(i => i.CreatedAt).ToListAsync();
        RootItems.Clear();
        var byParent = items.ToLookup(i => i.ParentId);
        foreach (var root in byParent[null])
        {
            var vm = Wrap(root, null);
            RootItems.Add(vm);
            AddChildren(vm);
        }
        void AddChildren(ListItemViewModel parent)
        {
            foreach (var child in byParent[parent.Entity.Id])
            {
                var vm = Wrap(child, parent);
                parent.Children.Add(vm);
                AddChildren(vm);
            }
        }
    }
    private bool _refreshQueued;
    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewTitle);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var entity = new ListItem { Title = NewTitle.Trim() };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Items.Add(entity);
        await db.SaveChangesAsync();

        RootItems.Add(Wrap(entity));
        NewTitle = string.Empty;
    }
    [RelayCommand]
    private async Task DeleteAsync(ListItemViewModel item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Items.Where(i => i.Id == item.Entity.Id).ExecuteDeleteAsync();
        (item.Parent?.Children ?? RootItems).Remove(item);
    }
    [RelayCommand]
    private async Task AddChild(ListItemViewModel parent)
    {
        var entity = new ListItem { Title = "New Item", ParentId = parent.Entity.Id };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Items.Add(entity);
        await db.SaveChangesAsync();
        var vm = Wrap(entity, parent);
        vm.BeginEditCommand.Execute(null);
        parent.Children.Add(vm);
        parent.IsExpanded = true;

    }

    private ListItemViewModel Wrap(ListItem entity, ListItemViewModel? parent = null)
    {
        var vm = new ListItemViewModel(entity, parent);
        // Adding/removing a child can change the leaf count without changing any IsDone.
        vm.Children.CollectionChanged += (_, _) => RefreshProperties();
        vm.DoneChanged += async changed =>
        {
            RefreshProperties();
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.Items
                .Where(i => i.Id == changed.Entity.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsDone, changed.IsDone));
        };
        vm.TitleChanged += async changed =>
        {
            RefreshProperties();
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.Items
                .Where(i => i.Id == changed.Entity.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Title, changed.Title));
        };
        return vm;
    }
    private void RefreshProperties()
    {
        if (_refreshQueued)
            return;
        _refreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _refreshQueued = false;
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(LeafCount));
            UpdateVisibleItems();
        });
    }
}
public enum ItemFilter
{
    All,
    Active,
    Done
}