using CyberList.Data.Entities;
using CyberList.ViewModels;

namespace CyberList.Tests;

public class ListItemViewModelTests
{
    // Builds a node and attaches it to its parent, the same way ListPageViewModel does.
    private static ListItemViewModel Node(string title, ListItemViewModel? parent = null, bool done = false)
    {
        var vm = new ListItemViewModel(new ListItem { Title = title, IsDone = done }, parent);
        parent?.Children.Add(vm);
        return vm;
    }

    // --- Tri-state checkbox ----------------------------------------------------------------

    [Fact]
    public void Leaf_CheckState_follows_IsDone()
    {
        var leaf = Node("task", done: true);

        Assert.True(leaf.IsLeaf);
        Assert.True(leaf.CheckState);
    }

    [Fact]
    public void Parent_is_indeterminate_when_only_some_children_are_done()
    {
        var root = Node("root");
        Node("a", root, done: true);
        Node("b", root);

        Assert.Null(root.CheckState);
        Assert.False(root.IsDone);
    }

    [Fact]
    public void Checking_a_parent_checks_every_descendant()
    {
        var root = Node("root");
        var folder = Node("folder", root);
        var leaf1 = Node("leaf1", folder);
        var leaf2 = Node("leaf2", root);

        root.CheckState = true;

        Assert.All([root, folder, leaf1, leaf2], n => Assert.True(n.IsDone));
    }

    [Fact]
    public void Checking_the_last_open_child_completes_all_ancestors()
    {
        var root = Node("root");
        var folder = Node("folder", root);
        Node("done", folder, done: true);
        var last = Node("last", folder);

        last.CheckState = true;

        Assert.True(folder.IsDone);
        Assert.True(root.IsDone);
        Assert.True(root.CheckState);
    }

    [Fact]
    public void Unchecking_a_child_reopens_its_parent()
    {
        var root = Node("root");
        var a = Node("a", root);
        Node("b", root);
        root.CheckState = true;

        a.CheckState = false;

        Assert.False(root.IsDone);
        Assert.Null(root.CheckState);
    }

    [Fact]
    public void DoneChanged_is_raised_so_the_change_can_be_saved()
    {
        var leaf = Node("leaf");
        var raised = new List<ListItemViewModel>();
        leaf.DoneChanged += raised.Add;

        leaf.CheckState = true;

        Assert.Equal([leaf], raised);
    }

    // --- Counts -----------------------------------------------------------------------------

    [Fact]
    public void LeafSummary_counts_only_leaves_at_any_depth()
    {
        var root = Node("root");
        var folder = Node("folder", root);    // a folder: not counted itself
        Node("x", folder, done: true);
        Node("y", folder);
        Node("z", root, done: true);

        Assert.Equal("2/3", root.LeafSummary);
        Assert.Equal("1/2", folder.LeafSummary);
    }

    // --- Tree shape (connector lines) -------------------------------------------------------

    [Fact]
    public void Depth_and_IsLastChild_describe_the_position_in_the_tree()
    {
        var root = Node("root");
        var first = Node("first", root);
        var second = Node("second", root);
        var grandchild = Node("grandchild", first);

        Assert.Equal(0, root.Depth);
        Assert.Equal(2, grandchild.Depth);
        Assert.False(first.IsLastChild);
        Assert.True(second.IsLastChild);
    }

    [Fact]
    public void GuideLines_mark_ancestor_levels_that_still_have_siblings_below()
    {
        //  root
        //  ├─ a          ← has a sibling below, so level 0 keeps a line going down past a's children
        //  │  └─ a1      ← last child: nothing continues in level 1
        //  │     └─ a1x  ← GuideLines = [true, false, false]
        //  └─ b             (the last slot is a1x's own elbow column, drawn separately)
        var root = Node("root");
        var a = Node("a", root);
        Node("b", root);
        var a1 = Node("a1", a);
        var a1x = Node("a1x", a1);

        Assert.Equal([true, false, false], a1x.GuideLines);
    }

    [Fact]
    public void Adding_a_sibling_notifies_the_old_last_child_to_redraw()
    {
        var root = Node("root");
        var a = Node("a", root);
        var changed = new List<string?>();
        a.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Node("b", root);

        Assert.False(a.IsLastChild);
        Assert.Contains(nameof(ListItemViewModel.IsLastChild), changed);
    }

    [Fact]
    public void A_folder_becomes_a_leaf_again_when_its_last_child_is_removed()
    {
        var root = Node("root");
        var child = Node("child", root);
        var changed = new List<string?>();
        root.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        root.Children.Remove(child);

        Assert.True(root.IsLeaf);
        Assert.Contains(nameof(ListItemViewModel.IsLeaf), changed);
    }

    // --- Inline edit ------------------------------------------------------------------------

    [Fact]
    public void SaveEdit_trims_and_updates_the_entity()
    {
        var item = Node("old");
        item.BeginEditCommand.Execute(null);
        item.EditTitle = "  new title  ";

        item.SaveEditCommand.Execute(null);

        Assert.Equal("new title", item.Title);
        Assert.Equal("new title", item.Entity.Title);
        Assert.False(item.IsEditing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("same")]
    public void SaveEdit_is_disabled_for_empty_or_unchanged_titles(string edit)
    {
        var item = Node("same");
        item.BeginEditCommand.Execute(null);
        item.EditTitle = edit;

        Assert.False(item.SaveEditCommand.CanExecute(null));
    }

    [Fact]
    public void CancelEdit_keeps_the_old_title()
    {
        var item = Node("keep");
        item.BeginEditCommand.Execute(null);
        item.EditTitle = "discard";

        item.CancelEditCommand.Execute(null);

        Assert.Equal("keep", item.Title);
        Assert.False(item.IsEditing);
    }
}
