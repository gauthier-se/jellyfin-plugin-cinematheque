using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A production country, with what the library holds of its cinema.
/// </summary>
/// <param name="Country">The country.</param>
/// <param name="FilmCount">The number of films in the library.</param>
/// <param name="Decades">The number of films per decade, oldest first.</param>
/// <param name="Directors">The directors with the most films from this country, most first.</param>
public sealed record CountrySummary(
    Country Country,
    int FilmCount,
    IReadOnlyList<DecadeCount> Decades,
    IReadOnlyList<string> Directors);
