using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A country summary as sent to the web client.
/// </summary>
/// <param name="Code">The country code.</param>
/// <param name="Name">The English name.</param>
/// <param name="FilmCount">The number of films.</param>
/// <param name="Decades">The number of films per decade, oldest first.</param>
/// <param name="Directors">The most represented directors.</param>
public sealed record CountrySummaryDto(
    string Code,
    string Name,
    int FilmCount,
    IReadOnlyList<DecadeDto> Decades,
    IReadOnlyList<string> Directors);
