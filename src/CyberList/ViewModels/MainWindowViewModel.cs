using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberList.Data;
using CyberList.Interfaces;
using CyberList.Services;

namespace CyberList.ViewModels;

/// <summary>
/// The shell: owns what stays on screen whatever page is open (title bar, header, date strip)
/// and the page currently shown in the content area.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public MainWindowViewModel(INavigationService navigationService, IEnumerable<PageInfo> pages)
    {
        _navigationService = navigationService;

        // Every page registered with AddPage, numbered in registration order: "01", "02"...
        NavItems = pages
            .Select((page, index) => new NavItem($"{index + 1:00}", page.Title, page.PageType))
            .ToList();

        _navigationService.CurrentPageChanged += (page) => CurrentPage = page;
        _navigationService.NavigateTo<ListPageViewModel>();
    }

    /// <summary>The page in the content area. The ViewLocator turns it into its view.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTabs), nameof(CurrentNavItem))]
    [NotifyCanExecuteChangedFor(nameof(NavigateCommand))]
    public partial ViewModelBase? CurrentPage { get; set; }

    /// <summary>For the header's "DATE / 2026.11.19" strip.</summary>
    public string Today => DateTime.Now.ToString("yyyy.MM.dd");
    public string DbName => Path.GetFileName(DbPaths.DefaultConnectionString).ToUpper();

    public IReadOnlyList<PageTab> PageTabs => (CurrentPage as IHasTabs)?.Tabs ?? Array.Empty<PageTab>();

    // --- Header navigation ----------------------------------------------------------------

    /// <summary>The entries of the header's navigation panel.</summary>
    public IReadOnlyList<NavItem> NavItems { get; }

    /// <summary>The entry for the page shown now: its title goes in the header, its number in "SCENE 01".</summary>
    public NavItem? CurrentNavItem => NavItems.FirstOrDefault(item => item.PageType == CurrentPage?.GetType());

    /// <summary>Whether the navigation panel under the header is expanded.</summary>
    [ObservableProperty]
    public partial bool IsNavOpen { get; set; }

    /// <summary>
    /// One command for every page: XAML passes the page type, e.g.
    /// CommandParameter="{x:Type vm:SettingsPageViewModel}" (or a NavItem's PageType).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void Navigate(Type pageType)
    {
        _navigationService.NavigateTo(pageType);
        IsNavOpen = false;
    }

    /// <summary>The current page's entry is disabled, which is also how the panel shows where you are.</summary>
    private bool CanNavigate(Type? pageType) => pageType is not null && pageType != CurrentPage?.GetType();
}
