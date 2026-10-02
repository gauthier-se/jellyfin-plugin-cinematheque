using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A reference to a person, as written in a URL or in a movement definition.
/// </summary>
/// <remarks>
/// Accepted forms:
/// <list type="bullet">
/// <item><c>tmdb:25236</c> matches that TMDB person only.</item>
/// <item><c>Johnnie To (tmdb:25236)</c> matches the TMDB person or the name, which keeps a
/// movement working when a credit has no TMDB id.</item>
/// <item><c>name:johnnieto</c> or <c>Johnnie To</c> matches the name, compared with
/// <see cref="Names.Key"/>.</item>
/// </list>
/// </remarks>
public sealed partial class PersonRef
{
    /// <summary>
    /// The prefix of a TMDB reference.
    /// </summary>
    public const string TmdbPrefix = "tmdb:";

    /// <summary>
    /// The prefix of a name reference.
    /// </summary>
    public const string NamePrefix = "name:";

    private const int MaxLength = 256;

    private PersonRef(string? tmdbId, string nameKey)
    {
        TmdbId = tmdbId;
        NameKey = nameKey;
    }

    /// <summary>
    /// Gets the TMDB person id, if the reference has one.
    /// </summary>
    public string? TmdbId { get; }

    /// <summary>
    /// Gets the name key, or an empty string for a TMDB-only reference.
    /// </summary>
    public string NameKey { get; }

    /// <summary>
    /// Parses a reference.
    /// </summary>
    /// <param name="value">The reference.</param>
    /// <returns>The reference, or <c>null</c> when the value names nobody.</returns>
    public static PersonRef? Parse(string? value)
    {
        string text = value?.Trim() ?? string.Empty;
        if (text.Length > MaxLength)
        {
            // References come from URLs: keep the patterns away from arbitrarily long input.
            return null;
        }

        Match tmdbOnly = TmdbOnlyPattern().Match(text);
        if (tmdbOnly.Success)
        {
            return new PersonRef(tmdbOnly.Groups["id"].Value, string.Empty);
        }

        Match withName = NameWithTmdbPattern().Match(text);
        if (withName.Success)
        {
            return new PersonRef(withName.Groups["id"].Value, Names.Key(withName.Groups["name"].Value));
        }

        if (text.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            text = text[NamePrefix.Length..];
        }

        string key = Names.Key(text);
        return key.Length > 0 ? new PersonRef(null, key) : null;
    }

    /// <summary>
    /// Tells whether a credit refers to this person.
    /// </summary>
    /// <param name="credit">The credit.</param>
    /// <returns><c>true</c> when the TMDB id or the name matches.</returns>
    public bool Matches(Credit credit)
    {
        ArgumentNullException.ThrowIfNull(credit);

        return (TmdbId is not null && string.Equals(credit.TmdbId, TmdbId, StringComparison.Ordinal))
            || (NameKey.Length > 0 && string.Equals(credit.NameKey, NameKey, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"^tmdb:\s*(?<id>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TmdbOnlyPattern();

    [GeneratedRegex(@"^(?<name>.+?)\s*\(\s*tmdb:\s*(?<id>\d+)\s*\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NameWithTmdbPattern();
}
