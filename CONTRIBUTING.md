# Contributing

Thanks for your interest in Cinematheque. Bug reports, movement definitions and code are all
welcome.

## Reporting a bug

Open an issue with the bug report template. The Jellyfin version, the web client layout and the
server log lines that mention `Cinematheque` or `FileTransformation` make most problems quick
to track down.

## Suggesting a movement

The default movements are meant to be accurate rather than exhaustive: a country, a period and
the directors who defined it. To propose one, open a feature request with its JSON definition
(see the README) and a source for the director list.

## Development setup

```sh
nix develop          # or install the .NET 10 SDK and Node.js 22
dotnet build
dotnet test
```

The solution has two projects:

- `src/Jellyfin.Plugin.Cinematheque`: the plugin.
  - `Catalog/`: the domain (films, countries, movements). No Jellyfin types, fully unit tested.
  - `Library/`: builds the catalog from Jellyfin's library.
  - `Api/`: the REST API and the embedded web assets.
  - `Integration/`: the File Transformation registration and the `index.html` patch.
  - `Web/`: the web client code, plain JavaScript and CSS with no build step.
- `tests/Jellyfin.Plugin.Cinematheque.Tests`: xUnit tests.

## Testing on a server

Build, then copy the DLL into a plugin folder on a test server and restart it:

```sh
dotnet build -c Release
scp src/Jellyfin.Plugin.Cinematheque/bin/Release/net10.0/Jellyfin.Plugin.Cinematheque.dll \
  root@jellyfin:/var/lib/jellyfin/plugins/Cinematheque_0.1.0.0/
ssh root@jellyfin systemctl restart jellyfin
```

Web client changes need a hard reload of the browser tab. Please try both the modern layout and
a legacy one (user settings, **Display**) when you touch `loader.js`.

## Code style

- The build treats warnings as errors and runs StyleCop and the .NET analyzers, as in the
  official Jellyfin plugin template. `dotnet build` must stay warning free.
- `.editorconfig` sets formatting for every file type.
- Build DOM nodes with the `h()` helper in `app.js`, never with `innerHTML` and library data.
- Comments explain why, not what.
- Code, comments and commit messages are in English. User-facing strings go through `t()` in
  `app.js` with both English and French entries.

## Pull requests

1. Fork and branch from `main`.
2. Add tests for catalog changes.
3. Make sure `dotnet build -c Release` and `dotnet test` pass.
4. Describe what you changed and how you tested it.

## Releasing

Maintainers only:

1. Update `version` and `changelog` in `build.yaml`, and the versions in `Directory.Build.props`.
2. Add the release to `CHANGELOG.md`.
3. Tag and push: `git tag v0.2.0 && git push origin v0.2.0`.

The release workflow builds the zip, creates the GitHub release and updates the plugin
repository manifest on the `gh-pages` branch.

## License

By contributing you agree that your contributions are licensed under the GPL-3.0.
