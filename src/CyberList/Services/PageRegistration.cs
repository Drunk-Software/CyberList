using CyberList.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyberList.Services;

/// <summary>A page the header's navigation panel can list: its title and its view model type.</summary>
public sealed record PageInfo(string Title, Type PageType);

public static class PageRegistration
{
    /// <summary>
    /// Registers a page: its view model as a singleton (pages keep their state between visits)
    /// plus a <see cref="PageInfo"/> so it shows up in the navigation panel. Pages are listed
    /// in the order they're registered.
    /// </summary>
    public static IServiceCollection AddPage<TPage>(this IServiceCollection services, string title)
        where TPage : ViewModelBase
    {
        services.AddSingleton<TPage>();
        services.AddSingleton(new PageInfo(title, typeof(TPage)));
        return services;
    }
}
