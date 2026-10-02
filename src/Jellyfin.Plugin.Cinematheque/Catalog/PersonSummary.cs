using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A person in one role, with what the library holds of their work.
/// </summary>
/// <param name="Key">The identity, as in <see cref="Credit.Key"/>.</param>
/// <param name="TmdbId">The TMDB person id, when known.</param>
/// <param name="Name">The display name: the spelling credited most often.</param>
/// <param name="Spellings">Every spelling they are credited under, the display name included.</param>
/// <param name="FilmIds">Their films in the library.</param>
/// <param name="FirstYear">The earliest production year among those films.</param>
/// <param name="LastYear">The latest production year among those films.</param>
/// <param name="Countries">The countries they worked in most, most frequent first.</param>
public sealed record PersonSummary(
    string Key,
    string? TmdbId,
    string Name,
    IReadOnlyList<string> Spellings,
    IReadOnlyList<Guid> FilmIds,
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<Country> Countries)
{
    // Computed once: every search compares them for every person.
    private readonly string[] _spellingKeys = [.. Spellings.Select(Names.Key)];

    /// <summary>
    /// Gets the number of films in the library.
    /// </summary>
    public int FilmCount => FilmIds.Count;

    /// <summary>
    /// Gets the comparison key of the display name, which also sorts it: "Éric Rohmer" among the
    /// E, not after Z. See <see cref="Names.Key"/>.
    /// </summary>
    public string NameKey { get; } = Names.Key(Name);

    /// <summary>
    /// Tells whether any spelling of the name contains a search text.
    /// </summary>
    /// <param name="search">The search text.</param>
    /// <returns><c>true</c> when a spelling matches, ignoring accents, case and punctuation.</returns>
    public bool NameContains(string search)
    {
        string key = Names.Key(search);
        return _spellingKeys.Any(s => s.Contains(key, StringComparison.Ordinal));
    }
}
