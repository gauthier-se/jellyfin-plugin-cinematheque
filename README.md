# Cinematheque

A Jellyfin plugin for film lovers. It adds a **Cinematheque** tab to the web client to
browse your film library the way a cinematheque programs it: by director, by actor,
by national cinema and by film movement.

[![CI](https://github.com/gauthier-se/jellyfin-plugin-cinematheque/actions/workflows/ci.yml/badge.svg)](https://github.com/gauthier-se/jellyfin-plugin-cinematheque/actions/workflows/ci.yml)
[![License: GPL v3](https://img.shields.io/badge/license-GPLv3-blue.svg)](LICENSE)

## Features

- **Directors.** Every director in your library with their portrait, number of films,
  active years and main countries. Open one to see their filmography in your library,
  oldest first, filterable by decade.
- **Actors.** The same for actors. Only the top of the bill counts (10 names per film by
  default), so the list shows leading players rather than every extra.
- **Countries.** National cinemas ranked by size, each with a decade histogram and its
  leading directors. Hong Kong, Japan, Italy, the Soviet Union: historical states stay
  separate from their successors.
- **Movements.** Curated movements such as German Expressionism, Italian Neorealism, the
  French New Wave, the Japanese New Wave, the Hong Kong New Wave or Taiwan New Cinema,
  matched against your library by country, period and director. Administrators can edit
  them or add their own.

Everything respects each user's library access and parental controls. The interface is
available in English and French, and country names follow the user's language.

## Requirements

- Jellyfin **12.1**
- The [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)
  plugin, which lets plugins add to the web client without modifying its files.
  Without it the API still works, but the tab does not appear.
- Production countries in your metadata. The default TMDB provider fills them in;
  films without them only show up under directors and actors.

## Installation

1. In **Dashboard > Plugins > Repositories**, add both repositories:
   - File Transformation: `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`
   - Cinematheque: `https://gauthier-se.github.io/jellyfin-plugin-cinematheque/manifest.json`
2. In **Catalog**, install **File Transformation**, then **Cinematheque**.
3. Restart the server, then reload the web client.

The tab appears next to **Favorites** in the header, or in the navigation drawer on small
screens and in the legacy layouts.

### Manual installation

Download the zip from the [latest release](https://github.com/gauthier-se/jellyfin-plugin-cinematheque/releases/latest),
extract it into a `Cinematheque_<version>` folder inside your Jellyfin `plugins` directory
(`/var/lib/jellyfin/plugins` on Debian), and restart the server.

## Configuration

**Dashboard > Plugins > Cinematheque** sets how many actors per film count, the default
minimum number of films for the people lists, and the movements.

A movement is a JSON object. A film belongs to it when its TMDB id is listed in `TmdbIds`,
or when it matches every rule that is set:

```json
{
  "Id": "shaw-brothers-wuxia",
  "Name": "Shaw Brothers wuxia",
  "Description": "Swordplay epics from the Shaw Brothers studio.",
  "Countries": ["HK"],
  "YearFrom": 1965,
  "YearTo": 1985,
  "Directors": ["Chang Cheh", "King Hu", "Chor Yuen", "Lau Kar-leung"],
  "Genres": ["Action"],
  "Tags": [],
  "TmdbIds": []
}
```

- `Countries`: ISO codes (`HK`) or English names (`Hong Kong`); any of them qualifies.
- `YearFrom` and `YearTo`: inclusive bounds; either can be left out.
- `Directors`, `Genres`, `Tags`: any of them qualifies. Names are compared without accents
  or punctuation, so `Nagisa Oshima` matches `Nagisa Ōshima`.
- An empty list means "no rule". A movement with no country, director, genre or tag rule
  only matches its `TmdbIds`.

## API

The tab is a client for a small REST API, available to any authenticated user:

| Endpoint | Description |
|----------|-------------|
| `GET /Cinematheque/Directors` | Directors, with `search`, `minFilms`, `sortBy` (`count` or `name`), `startIndex`, `limit` |
| `GET /Cinematheque/Actors` | Actors, same parameters |
| `GET /Cinematheque/Countries` | Countries with film counts, decades and leading directors |
| `GET /Cinematheque/Movements` | Movements with film counts |
| `GET /Cinematheque/Films` | Films filtered by `country`, `director`, `actor`, `movement`, `decade` |

## How it works

The server side builds an in-memory catalog of the films each user can see, from Jellyfin's
own metadata: production locations, people and genres. It is rebuilt after any library change.

On the web side, File Transformation adds one script tag to `index.html`. That loader adds the
tab to whichever layout is active and loads the app when the tab is opened. The app lives on
the home route (`#/home?cinematheque=directors`), so the header, drawer, back button and
bookmarks all keep working without registering a route in jellyfin-web.

## Development

The repository ships a Nix devshell with the .NET 10 SDK and Node.js:

```sh
nix develop
dotnet build
dotnet test
```

Without Nix, install the .NET 10 SDK. See [CONTRIBUTING.md](CONTRIBUTING.md) for testing against
a real server.

## License

[GPL-3.0](LICENSE), like Jellyfin and most of its plugins.

Cinematheque is not affiliated with the Jellyfin project or with any film archive.
