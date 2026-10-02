using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A director or actor, with what the library holds of their work.
/// </summary>
/// <param name="Name">The name as credited.</param>
/// <param name="FilmCount">The number of films in the library.</param>
/// <param name="FirstYear">The earliest production year among those films.</param>
/// <param name="LastYear">The latest production year among those films.</param>
/// <param name="Countries">The countries they worked in most, most frequent first.</param>
public sealed record PersonSummary(
    string Name,
    int FilmCount,
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<Country> Countries);
