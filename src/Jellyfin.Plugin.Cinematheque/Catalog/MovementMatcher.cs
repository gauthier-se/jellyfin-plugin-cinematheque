using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// Decides which films belong to a movement. See <see cref="MovementDefinition"/> for the rules.
/// </summary>
public sealed class MovementMatcher
{
    private readonly HashSet<string> _countries;
    private readonly HashSet<string> _directors;
    private readonly HashSet<string> _genres;
    private readonly HashSet<string> _tags;
    private readonly HashSet<string> _tmdbIds;
    private readonly int? _yearFrom;
    private readonly int? _yearTo;
    private readonly bool _hasRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovementMatcher"/> class.
    /// </summary>
    /// <param name="movement">The movement to match against.</param>
    public MovementMatcher(MovementDefinition movement)
    {
        ArgumentNullException.ThrowIfNull(movement);

        Movement = movement;
        // Accept "FR" as well as "France" in the configuration.
        _countries = KeySet((movement.Countries ?? []).Select(c => Country.FromLocation(c)?.Code ?? string.Empty));
        _directors = KeySet(movement.Directors);
        _genres = KeySet(movement.Genres);
        _tags = KeySet(movement.Tags);
        _tmdbIds = new HashSet<string>(
            (movement.TmdbIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
            StringComparer.Ordinal);
        _yearFrom = movement.YearFrom;
        _yearTo = movement.YearTo;
        _hasRules = _countries.Count > 0 || _directors.Count > 0 || _genres.Count > 0 || _tags.Count > 0;
    }

    /// <summary>
    /// Gets the movement this matcher was built from.
    /// </summary>
    public MovementDefinition Movement { get; }

    /// <summary>
    /// Tells whether a film belongs to the movement.
    /// </summary>
    /// <param name="film">The film.</param>
    /// <returns><c>true</c> when the film belongs to the movement.</returns>
    public bool Matches(Film film)
    {
        ArgumentNullException.ThrowIfNull(film);

        if (film.TmdbId is not null && _tmdbIds.Contains(film.TmdbId))
        {
            return true;
        }

        if (!_hasRules)
        {
            return false;
        }

        if (_yearFrom is not null || _yearTo is not null)
        {
            if (film.Year is not int year || year < (_yearFrom ?? int.MinValue) || year > (_yearTo ?? int.MaxValue))
            {
                return false;
            }
        }

        return MatchesAny(_countries, film.Countries.Select(c => c.Code))
            && MatchesAny(_directors, film.Directors)
            && MatchesAny(_genres, film.Genres)
            && MatchesAny(_tags, film.Tags);
    }

    private static bool MatchesAny(HashSet<string> wanted, IEnumerable<string> values)
        => wanted.Count == 0 || values.Any(value => wanted.Contains(Names.Key(value)));

    private static HashSet<string> KeySet(IEnumerable<string>? values)
        => new HashSet<string>(
            (values ?? []).Select(Names.Key).Where(key => key.Length > 0),
            StringComparer.Ordinal);
}
