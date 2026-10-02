namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// The filters a film list can be narrowed with. Unset filters are ignored.
/// </summary>
/// <param name="Country">A country code.</param>
/// <param name="Person">A person, in any form <see cref="PersonRef.Parse"/> accepts.</param>
/// <param name="Role">The role <paramref name="Person"/> must be credited in; any role when unset.</param>
/// <param name="Movement">A movement id.</param>
/// <param name="Decade">A decade, such as 1960.</param>
public sealed record FilmFilter(
    string? Country = null,
    string? Person = null,
    PersonRole? Role = null,
    string? Movement = null,
    int? Decade = null);
