# CyberList

Avalonia 12 + CommunityToolkit.Mvvm + EF Core 10 (SQLite), targeting .NET 10.

## Layout

```
src/
  CyberList/          Avalonia desktop app (Views, ViewModels, DI in App.axaml.cs)
  CyberList.Data/     EF Core: AppDbContext, entities, migrations
Directory.Packages.props   central package versions
dotnet-tools.json          local dotnet-ef tool
```

The database lives at `%LOCALAPPDATA%\CyberList\cyberlist.db`. Pending migrations are applied automatically at startup.

## Commands

```bash
dotnet tool restore
dotnet run --project src/CyberList
```

Add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project src/CyberList.Data --output-dir Migrations
```

Remove the last (unapplied) migration:

```bash
dotnet ef migrations remove --project src/CyberList.Data
```
