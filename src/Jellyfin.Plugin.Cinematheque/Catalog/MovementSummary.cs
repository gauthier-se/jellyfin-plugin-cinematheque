using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A movement, with the films of the library that belong to it.
/// </summary>
/// <param name="Movement">The movement.</param>
/// <param name="FilmIds">The matching films.</param>
public sealed record MovementSummary(MovementDefinition Movement, IReadOnlyList<Guid> FilmIds)
{
    /// <summary>
    /// Gets the number of matching films.
    /// </summary>
    public int FilmCount => FilmIds.Count;
}
