using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A film as the catalog sees it: only the fields used for browsing, already normalized.
/// </summary>
/// <param name="Id">The Jellyfin item id.</param>
/// <param name="Name">The display title.</param>
/// <param name="SortName">The title used for alphabetical sorting.</param>
/// <param name="Year">The production year, when known.</param>
/// <param name="Countries">The production countries, normalized.</param>
/// <param name="Genres">The genres as stored by Jellyfin.</param>
/// <param name="Tags">The tags as stored by Jellyfin.</param>
/// <param name="TmdbId">The TMDB id, when the film has one.</param>
/// <param name="Directors">The directors, in credit order.</param>
/// <param name="Actors">The actors kept for the catalog, in billing order.</param>
public sealed record Film(
    Guid Id,
    string Name,
    string SortName,
    int? Year,
    IReadOnlyList<Country> Countries,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags,
    string? TmdbId,
    IReadOnlyList<string> Directors,
    IReadOnlyList<string> Actors)
{
    /// <summary>
    /// Gets the decade the film was made in, such as 1960, when the year is known.
    /// </summary>
    public int? Decade => Year / 10 * 10;
}
