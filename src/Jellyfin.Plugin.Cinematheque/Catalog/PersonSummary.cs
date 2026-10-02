using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A person in one role, with what the library holds of their work.
/// </summary>
/// <param name="Key">The identity, as in <see cref="Credit.Key"/>.</param>
/// <param name="TmdbId">The TMDB person id, when known.</param>
/// <param name="Name">The display name: the spelling credited most often.</param>
/// <param name="FilmCount">The number of films in the library.</param>
/// <param name="FirstYear">The earliest production year among those films.</param>
/// <param name="LastYear">The latest production year among those films.</param>
/// <param name="Countries">The countries they worked in most, most frequent first.</param>
public sealed record PersonSummary(
    string Key,
    string? TmdbId,
    string Name,
    int FilmCount,
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<Country> Countries);
