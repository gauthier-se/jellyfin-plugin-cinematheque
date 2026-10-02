namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// The number of films made in one decade.
/// </summary>
/// <param name="Decade">The decade, such as 1960.</param>
/// <param name="FilmCount">The number of films.</param>
public sealed record DecadeCount(int Decade, int FilmCount);
