using CyberList.Services;
using CyberList.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyberList.Tests;

public class NavigationServiceTests
{
    // Two bare pages: navigation doesn't care what a page contains.
    private sealed class PageA : ViewModelBase;
    private sealed class PageB : ViewModelBase;

    private readonly NavigationService _nav;
    private readonly List<ViewModelBase> _raised = [];

    public NavigationServiceTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<PageA>()
            .AddSingleton<PageB>()
            .BuildServiceProvider();
        _nav = new NavigationService(services);
        _nav.CurrentPageChanged += _raised.Add;
    }

    [Fact]
    public void Starts_with_no_page_and_no_history()
    {
        Assert.Null(_nav.CurrentPage);
        Assert.False(_nav.CanGoBack);
    }

    [Fact]
    public void NavigateTo_shows_the_page_and_raises_CurrentPageChanged()
    {
        _nav.NavigateTo<PageA>();

        Assert.IsType<PageA>(_nav.CurrentPage);
        Assert.Equal([_nav.CurrentPage!], _raised);
    }

    [Fact]
    public void The_start_page_cannot_go_back()
    {
        _nav.NavigateTo<PageA>();

        Assert.False(_nav.CanGoBack);
    }

    [Fact]
    public void Navigating_to_the_page_already_shown_does_nothing()
    {
        _nav.NavigateTo<PageA>();
        _raised.Clear();

        _nav.NavigateTo<PageA>();

        Assert.Empty(_raised);
        Assert.False(_nav.CanGoBack);
    }

    [Fact]
    public void GoBack_returns_to_the_previous_page_and_raises_CurrentPageChanged()
    {
        _nav.NavigateTo<PageA>();
        var pageA = _nav.CurrentPage!;
        _nav.NavigateTo<PageB>();
        Assert.True(_nav.CanGoBack);
        _raised.Clear();

        _nav.GoBack();

        Assert.Same(pageA, _nav.CurrentPage);
        Assert.Equal([pageA], _raised);
        Assert.False(_nav.CanGoBack);
    }

    [Fact]
    public void GoBack_without_history_does_nothing()
    {
        _nav.NavigateTo<PageA>();
        _raised.Clear();

        _nav.GoBack();

        Assert.IsType<PageA>(_nav.CurrentPage);
        Assert.Empty(_raised);
    }

    [Fact]
    public void Pages_are_the_same_instance_on_every_visit()
    {
        _nav.NavigateTo<PageA>();
        var first = _nav.CurrentPage;
        _nav.NavigateTo<PageB>();

        _nav.NavigateTo<PageA>();

        Assert.Same(first, _nav.CurrentPage);
    }

    [Fact]
    public void NavigateTo_by_type_works_like_the_generic_overload()
    {
        _nav.NavigateTo(typeof(PageB));

        Assert.IsType<PageB>(_nav.CurrentPage);
    }

    [Fact]
    public void NavigateTo_by_type_rejects_types_that_are_not_pages()
    {
        Assert.Throws<ArgumentException>(() => _nav.NavigateTo(typeof(string)));
        Assert.Null(_nav.CurrentPage);
    }

    [Fact]
    public void NavigateTo_an_unregistered_page_throws()
    {
        // A page that was never registered with AddPage: a setup mistake, so fail loudly.
        Assert.Throws<InvalidOperationException>(() => _nav.NavigateTo<SettingsPageViewModel>());
    }
}
