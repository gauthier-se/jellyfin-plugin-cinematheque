using System;
using System.Linq;
using Jellyfin.Plugin.Cinematheque.Catalog;

namespace Jellyfin.Plugin.Cinematheque.Tests;

internal static class FilmFactory
{
    /// <summary>
    /// Builds a film. People are written "Name" or "Name@tmdbId".
    /// </summary>
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
            Credits(directors),
            Credits(actors));

    public static Credit Credit(string person)
    {
        string[] parts = person.Split('@');
        return new Credit(parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static Credit[] Credits(string[]? people) => (people ?? []).Select(Credit).ToArray();
}
