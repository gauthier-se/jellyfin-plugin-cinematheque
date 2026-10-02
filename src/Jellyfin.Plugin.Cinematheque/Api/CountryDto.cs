namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// A country as sent to the web client.
/// </summary>
/// <param name="Code">The code, used in URLs and for localization.</param>
/// <param name="Name">The English name, the fallback when the client cannot localize the code.</param>
public sealed record CountryDto(string Code, string Name);
