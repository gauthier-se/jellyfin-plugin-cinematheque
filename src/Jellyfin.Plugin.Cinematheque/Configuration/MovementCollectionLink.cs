using System;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// A Jellyfin collection the plugin created, for a movement or a person, and keeps in sync.
/// </summary>
/// <remarks>
/// Named after movements, its only use until 0.5: the XML configuration names array items after
/// their type, so renaming it would lose the links saved by earlier versions.
/// </remarks>
public class MovementCollectionLink
{
    /// <summary>
    /// The prefix of the collection keys of movements, before the movement id.
    /// </summary>
    public const string MovementPrefix = "movement:";

    /// <summary>
    /// Gets or sets what the collection mirrors: <c>movement:french-new-wave</c>, or a
    /// <see cref="CollectedPerson.CollectionKey"/>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the movement id, the only key saved before 0.5. Still written for movements,
    /// so an earlier version reinstalled over this one finds its collections.
    /// </summary>
    public string MovementId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection item id.
    /// </summary>
    public Guid CollectionId { get; set; }
}
