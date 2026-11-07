using System;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A person the administrator mirrors into a collection, as sent to the web client.
/// </summary>
/// <param name="Role">The role: <c>directors</c>, <c>actors</c> or <c>writers</c>.</param>
/// <param name="Key">The person key, as in <see cref="PersonDto.Key"/>, to remove them with.</param>
/// <param name="PersonId">The Jellyfin person item id, for the portrait.</param>
/// <param name="Name">The display name.</param>
/// <param name="FilmCount">The number of their films in that role.</param>
/// <param name="CollectionId">The collection item id, once it exists.</param>
/// <param name="Status">One of <c>Synced</c>, <c>Pending</c>, <c>NameTaken</c> or <c>NoFilms</c>.</param>
/// <param name="CollectionName">The name the collection has, or will be created with.</param>
public sealed record PersonCollectionDto(
    string Role,
    string Key,
    Guid PersonId,
    string Name,
    int FilmCount,
    Guid? CollectionId,
    string Status,
    string CollectionName);
