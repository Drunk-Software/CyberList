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
        _ = LoadAsync();
    }
    public ObservableCollection<ListItemViewModel> Items { get; } = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewTitle { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleItems))]
    public partial ItemFilter Filter { get; set; } = ItemFilter.All;
    public IEnumerable<ListItemViewModel> VisibleItems => Filter switch
    {
        ItemFilter.Active => Items.Where(i => !i.IsDone).ToList(),
        ItemFilter.Done => Items.Where(i => i.IsDone).ToList(),
        _ => Items.ToList()
    };
    public string Summary => $"{Items.Count(i => i.IsDone)} of {Items.Count} items";
    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.Items.AsNoTracking().OrderBy(i => i.CreatedAt).ToListAsync();
        Items.Clear();
        foreach (var item in items)
            Items.Add(Wrap(item));
        Items.CollectionChanged += (_, _) => RefreshProperties();
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewTitle);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var entity = new ListItem { Title = NewTitle.Trim() };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Items.Add(entity);
        await db.SaveChangesAsync();

        Items.Add(Wrap(entity));
        NewTitle = string.Empty;
    }
    [RelayCommand]
    private void SetFilter(ItemFilter filter) => Filter = filter;
    [RelayCommand]
    private async Task DeleteAsync(ListItemViewModel item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Items.Where(i => i.Id == item.Entity.Id).ExecuteDeleteAsync();
        Items.Remove(item);
    }

    private ListItemViewModel Wrap(ListItem entity)
    {
        var vm = new ListItemViewModel(entity);
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