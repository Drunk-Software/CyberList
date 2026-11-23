using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Material.Icons;

namespace CyberList.Views;

/// <summary>
/// The shell window: chrome, title bar and window-wide keys. Page-specific behaviour lives in
/// each page's own view (e.g. the list shortcuts are in <see cref="ListPageView"/>).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => RemoveWindowsFrameDecorations();
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
