namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// The number of films in one decade.
/// </summary>
/// <param name="Decade">The decade, such as 1960.</param>
/// <param name="FilmCount">The number of films.</param>
public sealed record DecadeDto(int Decade, int FilmCount);
