using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CyberList.ViewModels;

namespace CyberList.Views;

public partial class ListPageView : UserControl
{
    public ListPageView()
    {
        InitializeComponent();

        // Tunnel = we see the key on its way DOWN to the focused row, before the TreeView and
        // TreeViewItem get a chance to use it themselves (e.g. Space, which they'd swallow).
        Tree.AddHandler(KeyDownEvent, Tree_KeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Tabbing into the tree focuses a row without selecting it, but the shortcuts work on the
    /// SELECTED row. So: whatever row gets focus becomes the selected one.
    /// </summary>
    private void Tree_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is TreeViewItem { DataContext: ListItemViewModel item } && Tree.SelectedItem != item)
            Tree.SelectedItem = item;
    }

    private void EditBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: ListItemViewModel item })
            return;

        switch (e.Key)
        {
            case Key.Enter:
                if (item.SaveEditCommand.CanExecute(null))
                    item.SaveEditCommand.Execute(null);
                else
                    item.CancelEditCommand.Execute(null); // unchanged/empty title: just close the box
                e.Handled = true;
                FocusRow(item);
                break;

            case Key.Escape:
                item.CancelEditCommand.Execute(null);
                e.Handled = true;
                FocusRow(item);
                break;
        }
    }

    /// <summary>
    /// Give keyboard focus back to the row after editing, so E / A / Del keep working without
    /// reaching for the mouse. (The edit box is hidden now, so focus would otherwise be lost.)
    /// </summary>
    private void FocusRow(ListItemViewModel item)
    {
        Tree.SelectedItem = item;
        if (Tree.TreeContainerFromItem(item) is TreeViewItem row)
            row.Focus(NavigationMethod.Directional);
    }

    // --- Keyboard shortcuts on the selected row --------------------------------------------
    //   Space = check / uncheck the selected item
    //   E   = edit the selected item
    //   A   = add a child under it (the new row opens in edit mode, see AutoFocus)
    //   Del = delete it
    // Handled here (view) because it's about keys and focus; the actual work is still done by
    // the ViewModel's commands, exactly as if the row's buttons had been clicked.

    private void Tree_KeyDown(object? sender, KeyEventArgs e)
    {
        // Keys typed INTO a text box (the inline editor) bubble up to the tree too.
        // "e" there is a letter, not a shortcut, so ignore anything coming from a TextBox.
        if (e.Source is TextBox || e.KeyModifiers != KeyModifiers.None)
            return;
        if (Tree.SelectedItem is not ListItemViewModel item || DataContext is not ListPageViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Space:
                // Same as clicking the checkbox: a folder that's partly done becomes all done.
                item.CheckState = item.CheckState != true;
                e.Handled = true;
                break;

            case Key.E:
                item.BeginEditCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.A:
                vm.AddChildCommand.Execute(item);
                e.Handled = true;
                break;

            case Key.Delete:
                // Pick a neighbour to select afterwards so you can keep pressing Del.
                var siblings = (IList<ListItemViewModel>?)item.Parent?.Children ?? vm.RootItems;
                var index = siblings.IndexOf(item);
                var next = index + 1 < siblings.Count ? siblings[index + 1]
                         : index > 0 ? siblings[index - 1]
                         : item.Parent;

                _ = DeleteThenSelect(vm, item, next);
                e.Handled = true;
                break;
        }
    }

    private async Task DeleteThenSelect(ListPageViewModel vm, ListItemViewModel item, ListItemViewModel? next)
    {
        // Delete is async (it talks to the database): wait for it, so the row is really gone,
        // then let the tree update its rows before moving the selection.
        await vm.DeleteCommand.ExecuteAsync(item);
        if (next is not null)
            Dispatcher.UIThread.Post(() => FocusRow(next), DispatcherPriority.Loaded);
    }
}
