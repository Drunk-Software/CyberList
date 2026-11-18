using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CyberList.ViewModels;
using Material.Icons;

namespace CyberList.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => RemoveWindowsFrameDecorations();

        // Tunnel = we see the key on its way DOWN to the focused row, before the TreeView and
        // TreeViewItem get a chance to use it themselves (e.g. Space, which they'd swallow).
        Tree.AddHandler(KeyDownEvent, Tree_KeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>F1 anywhere opens the shortcuts flyout.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.F1)
        {
            HelpButton.Flyout?.ShowAt(HelpButton);
            e.Handled = true;
        }
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

    // --- Windows 11 frame tweaks ---------------------------------------------------------
    // WindowDecorations="BorderOnly" keeps the native resize frame, which Windows needs for
    // snapping (Win+arrows, drag-to-edge, snap tools). But Windows 11 then draws a thin
    // rounded border around the whole window rectangle, including our transparent glow
    // margin, which gives the real window edge away. These two DWM attributes turn it off.

    private const int DwmwaWindowCornerPreference = 33; // DWMWA_WINDOW_CORNER_PREFERENCE
    private const int DwmwaBorderColor = 34;            // DWMWA_BORDER_COLOR
    private const int DwmwcpDoNotRound = 1;             // DWMWCP_DONOTROUND
    private const int DwmwaColorNone = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void RemoveWindowsFrameDecorations()
    {
        // These attributes only exist on Windows 11 (build 22000+); elsewhere, do nothing.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return;
        if (TryGetPlatformHandle()?.Handle is not { } hwnd)
            return;

        var borderColor = DwmwaColorNone;
        DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref borderColor, sizeof(int));

        var corners = DwmwcpDoNotRound;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref corners, sizeof(int));
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
        if (Tree.SelectedItem is not ListItemViewModel item || DataContext is not MainWindowViewModel vm)
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

    private async Task DeleteThenSelect(MainWindowViewModel vm, ListItemViewModel item, ListItemViewModel? next)
    {
        // Delete is async (it talks to the database): wait for it, so the row is really gone,
        // then let the tree update its rows before moving the selection.
        await vm.DeleteCommand.ExecuteAsync(item);
        if (next is not null)
            Dispatcher.UIThread.Post(() => FocusRow(next), DispatcherPriority.Loaded);
    }

    // --- Custom caption buttons ---------------------------------------------------------
    // Minimizing/maximizing/closing is a pure view concern, so it lives here, not in a ViewModel.

    private void OnMinimize(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximize(object? sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object? sender, RoutedEventArgs e)
        => Close();

    /// <summary>
    /// Swap the maximize glyph for "restore" while maximized. Catches every way the state
    /// can change (our button, double-clicking the title bar, Win+Up, snapping...).
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty && MaximizeIcon is not null)
        {
            MaximizeIcon.Kind = WindowState == WindowState.Maximized
                ? MaterialIconKind.WindowRestore
                : MaterialIconKind.WindowMaximize;
        }
    }
}
