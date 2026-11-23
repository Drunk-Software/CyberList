using CyberList.ViewModels;

namespace CyberList.Tests;

public sealed class PageTabTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Selecting_a_tab_runs_its_action_once()
    {
        var calls = 0;
        var tab = new PageTab("01 ALL", () => calls++);

        tab.IsSelected = true;

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Deselecting_a_tab_does_not_run_its_action()
    {
        // When another tab is clicked, the old one is unchecked: that must not re-apply its filter.
        var calls = 0;
        var tab = new PageTab("01 ALL", () => calls++) { IsSelected = true };
        calls = 0;

        tab.IsSelected = false;

        Assert.Equal(0, calls);
    }

    [Fact]
    public void List_page_starts_on_the_ACTIVE_tab_matching_its_default_filter()
    {
        var page = new ListPageViewModel(_db);

        Assert.Equal(["01 ALL", "02 ACTIVE", "03 DONE"], page.Tabs.Select(t => t.Label));
        Assert.True(page.Tabs[1].IsSelected);
        Assert.Equal(ItemFilter.Active, page.Filter);
    }

    [Theory]
    [InlineData(0, ItemFilter.All)]
    [InlineData(1, ItemFilter.Active)]
    [InlineData(2, ItemFilter.Done)]
    public void Selecting_a_list_tab_sets_the_filter(int tabIndex, ItemFilter expected)
    {
        var page = new ListPageViewModel(_db);
        // Like the radio group: the old tab is unchecked before the new one is checked.
        // (Re-checking an already-checked tab is no change, so it wouldn't run the action.)
        foreach (var tab in page.Tabs)
            tab.IsSelected = false;
        page.Filter = expected == ItemFilter.All ? ItemFilter.Done : ItemFilter.All; // start somewhere else

        page.Tabs[tabIndex].IsSelected = true;

        Assert.Equal(expected, page.Filter);
    }
}
