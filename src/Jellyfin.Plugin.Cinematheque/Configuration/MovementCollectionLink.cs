using System;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// A Jellyfin collection the plugin created for a movement and keeps in sync.
/// </summary>
public class MovementCollectionLink
{
    /// <summary>
    /// Gets or sets the movement id.
    /// </summary>
    public string MovementId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection item id.
    /// </summary>
    public Guid CollectionId { get; set; }
}
