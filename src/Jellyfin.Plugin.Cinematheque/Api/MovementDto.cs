using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A movement as sent to the web client.
/// </summary>
/// <param name="Id">The movement id.</param>
/// <param name="Name">The display name.</param>
/// <param name="Description">The description.</param>
/// <param name="YearFrom">The first year.</param>
/// <param name="YearTo">The last year.</param>
/// <param name="Countries">The countries.</param>
/// <param name="FilmCount">The number of matching films in the library.</param>
/// <param name="SeenCount">How many of those films the user has watched.</param>
public sealed record MovementDto(
    string Id,
    string Name,
    string Description,
    int? YearFrom,
    int? YearTo,
    IReadOnlyList<CountryDto> Countries,
    int FilmCount,
    int SeenCount);
