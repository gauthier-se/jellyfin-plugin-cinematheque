# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow Jellyfin's
four-part plugin versioning.

## [Unreleased]

### Changed

- The library is read once for every user instead of once per user, and held in memory once.

### Removed

- `GET /Cinematheque/Directors` and `/Actors`, and the `director` and `actor` filters of
  `/Films`, kept from 0.1. Use `People/{role}` and the `person` and `role` filters.

### Fixed

- "Show more" no longer fails past 480 people or films. It adds the next page to the list
  instead of loading the whole list again.
- A movement collection is no longer left out of sync for good when a sync fails or is
  cancelled halfway. Collections are recorded as soon as they are created, and one failing
  movement no longer stops the others.
- When the configuration page fails to load the settings, saving no longer erases the added
  and edited movements. A failed save no longer leaves the page loading forever.
- Names sort ignoring accents: Éric Rohmer comes among the E, not after Z.

### Security

- Signing out and in as another user in the same browser tab no longer shows the previous
  user's lists, or a film from their libraries on the My Media tile.

## [0.2.0] - 2026-10-02

### Added

- People are identified by their TMDB id, so one person credited under several spellings or
  scripts (Johnnie To and 杜琪峯) is one entry, and homonyms stay apart. Movements accept
  `tmdb:` references, and the built-in ones carry the ids of their directors.
- Screenwriters tab.
- Watch progress on every person, country and movement, a check mark on watched posters and a
  "not seen yet" filter.
- "Pick a film for me": a random film you have not watched, from the list on screen.
- Countries can count co-productions under their first listed country only.
- Movements are translated; the built-ins ship in English and French.
- Movements are edited in a form on the configuration page, with JSON import and export.
- Optional collections, one per movement, kept in sync after each library scan and given a
  poster drawn from their films.
- A Cinematheque tile in My Media on the home page.
- Plugin logo in the catalog and on the dashboard.

### Changed

- Built-in movements now update with the plugin; the configuration only keeps your changes.
  Copies of built-ins saved by 0.1 are replaced by the current versions on upgrade.
- Person cards show the name and film count only; years and countries moved to the person page.
- Cards use Jellyfin's own hover effect.

### Fixed

- Movements never matched their `TmdbIds`: film TMDB ids were not loaded.
- Legacy layouts: the Home and Favorites tabs now close the Cinematheque view.
- A user whose library access was revoked kept seeing those films until the next library change.

## [0.1.0] - 2026-10-02

### Added

- Directors and actors lists with portraits, film counts, active years and main countries.
- Filmography of a director or actor in the library, filterable by decade.
- National cinemas with decade histograms and leading directors.
- Curated film movements, editable from the plugin configuration page.
- Cinematheque tab in the modern and legacy web client layouts, through File Transformation.
- English and French interface.

[Unreleased]: https://github.com/gauthier-se/jellyfin-plugin-cinematheque/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/gauthier-se/jellyfin-plugin-cinematheque/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/gauthier-se/jellyfin-plugin-cinematheque/releases/tag/v0.1.0
