namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Whether the collection of a person exists, and why not.
/// </summary>
public enum PersonCollectionStatus
{
    /// <summary>
    /// The collection exists and follows the person's films.
    /// </summary>
    Synced,

    /// <summary>
    /// Not created yet: the next sync will.
    /// </summary>
    Pending,

    /// <summary>
    /// Not created: another collection has its name, and Cinematheque leaves it alone.
    /// </summary>
    NameTaken,

    /// <summary>
    /// Not created: the person has no films in that role.
    /// </summary>
    NoFilms,
}
