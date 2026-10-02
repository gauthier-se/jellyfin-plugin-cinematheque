namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// The filters a film list can be narrowed with. Unset filters are ignored.
/// </summary>
/// <param name="Country">A country code.</param>
/// <param name="Director">A director name.</param>
/// <param name="Actor">An actor name.</param>
/// <param name="Movement">A movement id.</param>
/// <param name="Decade">A decade, such as 1960.</param>
public sealed record FilmFilter(
    string? Country = null,
    string? Director = null,
    string? Actor = null,
    string? Movement = null,
    int? Decade = null);
