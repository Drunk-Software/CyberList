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

CI checks the title on every PR. See [CONTRIBUTING.md](CONTRIBUTING.md). To release, run **Actions → Release → Run workflow** on `master`:
it tags `vX.Y.Z`, builds self-contained apps for Windows, Linux and macOS (x64 + arm64), and
attaches them to a GitHub release.

## Verifying a download

Each release includes a `SHA256SUMS` file and a GitHub build attestation for every package.

```bash
# The file is exactly what the release lists (run in the folder you downloaded to)
sha256sum -c SHA256SUMS --ignore-missing

# It was built by this repo's Release workflow, from a commit in this repo
gh attestation verify CyberList-<version>-<platform>.zip --repo Drunk-Software/CyberList
```

On Windows without these tools: `Get-FileHash CyberList-<version>-win-x64.zip` and compare
the hash with the matching line in `SHA256SUMS`.

## License

[PolyForm Noncommercial 1.0.0](LICENSE.txt). In short: you're free to use, fork, modify and
share CyberList for any **non-commercial** purpose (personal use, learning, hobby projects,
research, non-profits). Selling it, or using it to make money, needs permission from the author.
This is *source-available*, not OSI "open source".
