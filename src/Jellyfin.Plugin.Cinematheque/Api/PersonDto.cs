using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A director or actor as sent to the web client.
/// </summary>
/// <param name="Id">The Jellyfin person item id, for the portrait and the person page.</param>
/// <param name="Name">The name.</param>
/// <param name="FilmCount">The number of films in the library.</param>
/// <param name="FirstYear">The earliest production year.</param>
/// <param name="LastYear">The latest production year.</param>
/// <param name="Countries">The countries they worked in most.</param>
public sealed record PersonDto(
    Guid Id,
    string Name,
    int FilmCount,
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<CountryDto> Countries);
