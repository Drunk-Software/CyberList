using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace CyberList.Controls;

/// <summary>
/// An attached property ("behavior"): <c>ctl:AutoFocus.WhenVisible="True"</c> moves keyboard focus
/// into the element (and selects a TextBox's text) every time it appears on screen — when it
/// becomes visible, or when it's created already visible.
///
/// Why not do this in the ViewModel? Focus is a view-only concept; the ViewModel just says
/// "IsEditing = true" and the view decides what that looks like, including where the caret goes.
/// </summary>
public static class AutoFocus
{
    public static readonly AttachedProperty<bool> WhenVisibleProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("WhenVisible", typeof(AutoFocus));

    public static bool GetWhenVisible(InputElement element) => element.GetValue(WhenVisibleProperty);
    public static void SetWhenVisible(InputElement element, bool value) => element.SetValue(WhenVisibleProperty, value);

    static AutoFocus()
    {
        // Case 1: the element already exists and flips from hidden to visible (Edit pressed).
        Visual.IsVisibleProperty.Changed.AddClassHandler<InputElement>((element, _) => TryFocus(element));

        // Case 2: the element is created already visible (a new row that starts in edit mode).
        WhenVisibleProperty.Changed.AddClassHandler<InputElement>((element, e) =>
        {
            if (e.NewValue is true)
                element.AttachedToVisualTree += (_, _) => TryFocus(element);
        });
    }

    private static void TryFocus(InputElement element)
    {
        if (!GetWhenVisible(element) || !element.IsVisible)
            return;

        // Wait until layout has run: an element that isn't arranged yet can't take focus.
        Dispatcher.UIThread.Post(() =>
        {
            if (!element.IsEffectivelyVisible)
                return;
            element.Focus(NavigationMethod.Pointer);
            if (element is TextBox box)
                box.SelectAll();
        }, DispatcherPriority.Loaded);
    }
}
