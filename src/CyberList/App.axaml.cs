using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CyberList.Data;
using CyberList.Helpers;
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

            // The saved DB location is missing: open Settings, where the warning offers a way out.
            if (Services.GetRequiredService<IDatabaseService>().StartupWarning is not null)
                Services.GetRequiredService<INavigationService>().NavigateTo<SettingsPageViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Created on first use, which is the DB migration below: settings load before the DB opens.
        services.AddSingleton<ISettingsService>(
            _ => new JsonSettingsService(ConfigurationHelper.SettingsFilePath));

        // One instance behind both interfaces: it decides the DB path and creates the contexts.
        services.AddSingleton(sp => new DatabaseService(
            sp.GetRequiredService<ISettingsService>(),
            ConfigurationHelper.DefaultDatabasePath,
            ConfigurationHelper.BackupsFolder));
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<DatabaseService>());
        services.AddSingleton<IDbContextFactory<AppDbContext>>(sp => sp.GetRequiredService<DatabaseService>());
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();

        // Pages: one line each. The order here is the order in the header's navigation panel.
        services.AddPage<ListPageViewModel>("TASK GRID");
        services.AddPage<SettingsPageViewModel>("SETTINGS");
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<INavigationService, NavigationService>();

        return services.BuildServiceProvider();
    }
}
