using CyberList.Interfaces;
using CyberList.Services;
using CyberList.ViewModels;
using Microsoft.EntityFrameworkCore;
using CyberList.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CyberList.Tests;

/// <summary>
/// The shell, wired the same way App.ConfigureServices does it (real NavigationService and
/// AddPage registrations), over an empty in-memory database.
/// </summary>
public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ServiceProvider _services;
    private readonly MainWindowViewModel _shell;

    public MainWindowViewModelTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbContextFactory<AppDbContext>>(_db);
        services.AddPage<ListPageViewModel>("TASK GRID");
        services.AddPage<SettingsPageViewModel>("SETTINGS");
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<MainWindowViewModel>();
        _services = services.BuildServiceProvider();

        _shell = _services.GetRequiredService<MainWindowViewModel>();
    }

    public void Dispose()
    {
        _services.Dispose();
        _db.Dispose();
    }

    [Fact]
    public void AddPage_registers_the_page_as_a_singleton()
    {
        Assert.Same(_services.GetRequiredService<ListPageViewModel>(),
                    _services.GetRequiredService<ListPageViewModel>());
    }

    [Fact]
    public void Starts_on_the_list_page()
    {
        Assert.IsType<ListPageViewModel>(_shell.CurrentPage);
        Assert.Equal("TASK GRID", _shell.CurrentNavItem?.Title);
        Assert.Equal("01", _shell.CurrentNavItem?.Number);
    }

    [Fact]
    public void NavItems_list_every_page_numbered_in_registration_order()
    {
        Assert.Equal(
            [
                new NavItem("01", "TASK GRID", typeof(ListPageViewModel)),
                new NavItem("02", "SETTINGS", typeof(SettingsPageViewModel)),
            ],
            _shell.NavItems);
    }

    [Fact]
    public void Navigate_shows_the_page_and_closes_the_panel()
    {
        _shell.IsNavOpen = true;

        _shell.NavigateCommand.Execute(typeof(SettingsPageViewModel));

        Assert.IsType<SettingsPageViewModel>(_shell.CurrentPage);
        Assert.Equal("SETTINGS", _shell.CurrentNavItem?.Title);
        Assert.False(_shell.IsNavOpen);
    }

    [Fact]
    public void The_current_page_cannot_be_navigated_to_again()
    {
        Assert.False(_shell.NavigateCommand.CanExecute(typeof(ListPageViewModel)));
        Assert.True(_shell.NavigateCommand.CanExecute(typeof(SettingsPageViewModel)));
    }

    [Fact]
    public void Navigate_tells_the_header_to_refresh()
    {
        var changed = new List<string?>();
        _shell.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        var canExecuteChanged = false;
        _shell.NavigateCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        _shell.NavigateCommand.Execute(typeof(SettingsPageViewModel));

        Assert.Contains(nameof(MainWindowViewModel.CurrentPage), changed);
        Assert.Contains(nameof(MainWindowViewModel.CurrentNavItem), changed);
        Assert.Contains(nameof(MainWindowViewModel.PageTabs), changed);
        Assert.True(canExecuteChanged);
    }

    [Fact]
    public void Tabs_come_from_the_current_page_and_disappear_on_pages_without_tabs()
    {
        var list = (ListPageViewModel)_shell.CurrentPage!;
        Assert.Same(list.Tabs, _shell.PageTabs);

        _shell.NavigateCommand.Execute(typeof(SettingsPageViewModel));

        Assert.Empty(_shell.PageTabs);
    }

    [Fact]
    public void The_list_keeps_its_filter_when_you_come_back()
    {
        var list = (ListPageViewModel)_shell.CurrentPage!;
        list.Tabs[2].IsSelected = true;

        _shell.NavigateCommand.Execute(typeof(SettingsPageViewModel));
        _shell.NavigateCommand.Execute(typeof(ListPageViewModel));

        Assert.Same(list, _shell.CurrentPage);
        Assert.Equal(ItemFilter.Done, list.Filter);
    }
}
