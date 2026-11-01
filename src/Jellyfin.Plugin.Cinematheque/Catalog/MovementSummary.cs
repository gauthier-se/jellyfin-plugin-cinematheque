using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A movement, with the films of the library that belong to it.
/// </summary>
/// <param name="Movement">The movement.</param>
/// <param name="FilmIds">The matching films.</param>
/// <param name="Directors">The directors with the most matching films.</param>
/// <param name="EssentialCount">How many essential films the movement lists.</param>
/// <param name="MissingEssentials">The essential films that are not in the library.</param>
public sealed record MovementSummary(
    MovementDefinition Movement,
    IReadOnlyList<Guid> FilmIds,
    IReadOnlyList<PersonLink> Directors,
    int EssentialCount,
    IReadOnlyList<EssentialFilm> MissingEssentials)
{
    /// <summary>
    /// Gets the number of matching films.
    /// </summary>
    public int FilmCount => FilmIds.Count;
}
