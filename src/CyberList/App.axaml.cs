using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CyberList.Data;
using CyberList.Interfaces;
using CyberList.Services;
using CyberList.ViewModels;
using CyberList.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CyberList;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        // Apply any pending EF Core migrations (creates the SQLite file on first run).
        using (var db = Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
        {
            db.Database.Migrate();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(DbPaths.DefaultConnectionString));

        // Pages: one line each. The order here is the order in the header's navigation panel.
        services.AddPage<ListPageViewModel>("TASK GRID");
        services.AddPage<SettingsPageViewModel>("SETTINGS");
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<INavigationService, NavigationService>();

        return services.BuildServiceProvider();
    }
}
