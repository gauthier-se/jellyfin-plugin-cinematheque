using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A person in one role, as sent to the web client.
/// </summary>
/// <param name="Id">The Jellyfin person item id, for the portrait and the person page.</param>
/// <param name="Key">The identity, to pass back as the <c>person</c> filter.</param>
/// <param name="TmdbId">The TMDB person id, when known.</param>
/// <param name="Name">The display name.</param>
/// <param name="FilmCount">The number of films in the library.</param>
/// <param name="SeenCount">How many of those films the user has watched.</param>
/// <param name="FirstYear">The earliest production year.</param>
/// <param name="LastYear">The latest production year.</param>
/// <param name="Countries">The countries they worked in most.</param>
public sealed record PersonDto(
    Guid Id,
    string Key,
    string? TmdbId,
    string Name,
    int FilmCount,
    int SeenCount,
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<CountryDto> Countries);
