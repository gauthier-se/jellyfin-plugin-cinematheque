using System;
using Jellyfin.Plugin.Cinematheque.Catalog;

namespace Jellyfin.Plugin.Cinematheque.Tests;

internal static class FilmFactory
{
    public static Film Film(
        string name,
        int? year = null,
        string[]? countries = null,
        string[]? directors = null,
        string[]? actors = null,
        string[]? genres = null,
        string[]? tags = null,
        string? tmdbId = null)
        => new Film(
            Guid.NewGuid(),
            name,
            name,
            year,
            Country.FromLocations(countries ?? []),
            genres ?? [],
            tags ?? [],
            tmdbId,
            directors ?? [],
            actors ?? []);
}
