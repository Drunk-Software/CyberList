using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Data;
using CyberList.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CyberList.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MainWindowViewModel(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        RootItems.CollectionChanged += (_, _) => RefreshProperties();
        _ = LoadAsync();
    }
    public ObservableCollection<ListItemViewModel> RootItems { get; } = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewTitle { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleItems))]
    public partial ItemFilter Filter { get; set; } = ItemFilter.All;
    public IEnumerable<ListItemViewModel> VisibleItems => Filter switch
    {
        ItemFilter.Active => RootItems.Where(i => !i.IsDone).ToList(),
        ItemFilter.Done => RootItems.Where(i => i.IsDone).ToList(),
        _ => RootItems.ToList()
    };
    public string Summary => $"{RootItems.Count(i => i.IsDone)} of {RootItems.Count} items";
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
    private void SetFilter(ItemFilter filter) => Filter = filter;
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
        parent.Children.Add(vm);
    }

    private ListItemViewModel Wrap(ListItem entity, ListItemViewModel? parent = null)
    {
        var vm = new ListItemViewModel(entity, parent);
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
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(VisibleItems));
    }
}
public enum ItemFilter
{
    All,
    Active,
    Done
}