namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A person to show and link to.
/// </summary>
/// <param name="Key">The identity, to pass back as the <c>person</c> filter.</param>
/// <param name="Name">The display name.</param>
public sealed record PersonLinkDto(string Key, string Name);
