namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A person credited on a film.
/// </summary>
/// <param name="Name">The name as credited.</param>
/// <param name="TmdbId">The TMDB person id, when Jellyfin has one.</param>
public sealed record Credit(string Name, string? TmdbId)
{
    /// <summary>
    /// Gets the comparison key of the name. See <see cref="Names.Key"/>.
    /// </summary>
    public string NameKey { get; } = Names.Key(Name);

    /// <summary>
    /// Gets the identity of the person: their TMDB id when known, their name otherwise.
    /// </summary>
    /// <remarks>
    /// The TMDB id is what tells two people with the same name apart, and what brings together
    /// one person credited under different spellings or scripts ("Johnnie To" and "杜琪峯").
    /// </remarks>
    public string Key => TmdbId is null ? PersonRef.NamePrefix + NameKey : PersonRef.TmdbPrefix + TmdbId;
}
