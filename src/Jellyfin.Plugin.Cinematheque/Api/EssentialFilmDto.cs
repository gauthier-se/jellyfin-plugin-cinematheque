namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// An essential film of a movement, as sent to the web client.
/// </summary>
/// <param name="TmdbId">The TMDB id.</param>
/// <param name="Title">The title in the requested language.</param>
/// <param name="Year">The year.</param>
public sealed record EssentialFilmDto(string TmdbId, string Title, int? Year);
