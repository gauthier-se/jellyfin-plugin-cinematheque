using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A filtered film list, with the facets that remain available for narrowing it further.
/// </summary>
/// <param name="Items">The films on this page.</param>
/// <param name="TotalRecordCount">The number of matching films across all pages.</param>
/// <param name="Decades">The decades present in the full result, before the decade filter.</param>
public sealed record FilmPageDto(
    IReadOnlyList<FilmDto> Items,
    int TotalRecordCount,
    IReadOnlyList<DecadeDto> Decades);
