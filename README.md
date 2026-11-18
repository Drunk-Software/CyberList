# CyberList

Avalonia 12 + CommunityToolkit.Mvvm + EF Core 10 (SQLite), targeting .NET 10.

## Layout

```
src/
  CyberList/          Avalonia desktop app (Views, ViewModels, DI in App.axaml.cs)
  CyberList.Data/     EF Core: AppDbContext, entities, migrations
tests/
  CyberList.Tests/    xUnit: ViewModel logic, converter, migrations
.github/workflows/
  ci.yml              build + test on every PR
  release.yml         manual release: version, build all platforms, publish
Directory.Packages.props   central package versions
GitVersion.yml             how release versions are calculated
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

Run the tests:

```bash
dotnet test CyberList.slnx
```

## Pull requests and releases

PR titles (which become the squash-merge commit) decide the next version:

| PR title starts with | Bump  | Example       |
|----------------------|-------|---------------|
| `!feat:`             | major | 1.4.2 → 2.0.0 |
| `feat:`              | minor | 1.4.2 → 1.5.0 |
| `patch:`             | patch | 1.4.2 → 1.4.3 |

CI checks the title on every PR. To release, run **Actions → Release → Run workflow** on `master`:
it tags `vX.Y.Z`, builds self-contained apps for Windows, Linux and macOS (x64 + arm64), and
attaches them to a GitHub release.
