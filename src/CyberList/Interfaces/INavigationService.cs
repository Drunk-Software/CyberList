using CyberList.ViewModels;

namespace CyberList.Interfaces;

public interface INavigationService
{
    /// <summary>The page shown now. Null only before the first NavigateTo.</summary>
    ViewModelBase? CurrentPage { get; }

    /// <summary>True when there is a previous page to return to.</summary>
    bool CanGoBack { get; }

    /// <summary>Raised after every page change, with the new page.</summary>
    event Action<ViewModelBase>? CurrentPageChanged;

    /// <summary>Show the page of type T. Does nothing if it is already shown.</summary>
    void NavigateTo<T>() where T : ViewModelBase;

    /// <summary>
    /// Same as <see cref="NavigateTo{T}"/>, for when the page type is only known at runtime
    /// (e.g. a command parameter from XAML). Must be a registered ViewModelBase type.
    /// </summary>
    void NavigateTo(Type pageType);

    /// <summary>Return to the previous page. Does nothing if there is none.</summary>
    void GoBack();
}