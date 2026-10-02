# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow Jellyfin's
four-part plugin versioning.

## [Unreleased]

### Changed

- Built-in movements now update with the plugin; the configuration only keeps your changes.
  Copies of built-ins saved by 0.1 are replaced by the current versions on upgrade.

### Fixed

- Legacy layouts: the Home and Favorites tabs now close the Cinematheque view.

## [0.1.0] - 2026-10-02

### Added

- Directors and actors lists with portraits, film counts, active years and main countries.
- Filmography of a director or actor in the library, filterable by decade.
- National cinemas with decade histograms and leading directors.
- Curated film movements, editable from the plugin configuration page.
- Cinematheque tab in the modern and legacy web client layouts, through File Transformation.
- English and French interface.

[Unreleased]: https://github.com/gauthier-se/jellyfin-plugin-cinematheque/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/gauthier-se/jellyfin-plugin-cinematheque/releases/tag/v0.1.0
