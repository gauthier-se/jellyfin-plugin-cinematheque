using System;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Where the collection of a person stands.
/// </summary>
/// <param name="CollectionId">The collection item id, once it exists.</param>
/// <param name="CollectionName">The name it has, or will be created with.</param>
/// <param name="Status">The status.</param>
public sealed record PersonCollectionState(Guid? CollectionId, string CollectionName, PersonCollectionStatus Status);
