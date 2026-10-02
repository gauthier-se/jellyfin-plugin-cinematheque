namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// Enough to show a person and link to their page.
/// </summary>
/// <param name="Key">The identity, as in <see cref="Credit.Key"/>.</param>
/// <param name="Name">The display name.</param>
public sealed record PersonLink(string Key, string Name);
