using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A film as sent to the web client.
/// </summary>
/// <param name="Id">The Jellyfin item id.</param>
/// <param name="Name">The title.</param>
/// <param name="Year">The production year.</param>
/// <param name="Countries">The production countries.</param>
/// <param name="Directors">The directors.</param>
/// <param name="Seen">Whether the user has watched the film.</param>
public sealed record FilmDto(
    Guid Id,
    string Name,
    int? Year,
    IReadOnlyList<CountryDto> Countries,
    IReadOnlyList<string> Directors,
    bool Seen);
