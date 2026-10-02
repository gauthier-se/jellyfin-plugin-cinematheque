using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// An in-memory view of one user's films, indexed by person, country and movement.
/// </summary>
/// <remarks>
/// The catalog is immutable once built, so one instance can serve concurrent requests. People are
/// grouped by <see cref="Credit.Key"/>: their TMDB id when known, their name otherwise. A credit
/// without a TMDB id borrows one from another credit with the same name, as long as that name
/// leads to a single TMDB person.
/// </remarks>
public sealed class FilmCatalog
{
    private const int TopCountries = 3;
    private const int TopDirectors = 5;

    private readonly Dictionary<PersonRole, Lazy<IReadOnlyList<PersonSummary>>> _people;
    private readonly Lazy<IReadOnlyList<CountrySummary>> _countries;
    private readonly IReadOnlyList<MovementMatcher> _movements;
    private readonly Lazy<IReadOnlyList<(MovementDefinition Movement, int FilmCount)>> _movementCounts;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilmCatalog"/> class.
    /// </summary>
    /// <param name="films">The films visible to the user.</param>
    /// <param name="movements">The configured movements.</param>
    public FilmCatalog(IEnumerable<Film> films, IEnumerable<MovementDefinition> movements)
    {
        ArgumentNullException.ThrowIfNull(films);
        ArgumentNullException.ThrowIfNull(movements);

        Films = ShareTmdbIds(films.ToArray());
        _movements = movements
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => new MovementMatcher(m))
            .ToArray();
        _people = Enum.GetValues<PersonRole>().ToDictionary(
            role => role,
            role => new Lazy<IReadOnlyList<PersonSummary>>(() => Summarize(role)));
        _countries = new Lazy<IReadOnlyList<CountrySummary>>(SummarizeCountries);
        _movementCounts = new Lazy<IReadOnlyList<(MovementDefinition Movement, int FilmCount)>>(
            () => _movements.Select(m => (m.Movement, Films.Count(m.Matches))).ToArray());
    }

    /// <summary>
    /// Gets every film in the catalog.
    /// </summary>
    public IReadOnlyList<Film> Films { get; }

    /// <summary>
    /// Lists the people credited in a role, most prolific first.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The people.</returns>
    public IReadOnlyList<PersonSummary> GetPeople(PersonRole role) => _people[role].Value;

    /// <summary>
    /// Lists the production countries, most represented first.
    /// </summary>
    /// <returns>The countries.</returns>
    public IReadOnlyList<CountrySummary> GetCountries() => _countries.Value;

    /// <summary>
    /// Lists the configured movements with the number of films each one matches.
    /// </summary>
    /// <returns>The movements, in configuration order.</returns>
    public IReadOnlyList<(MovementDefinition Movement, int FilmCount)> GetMovements() => _movementCounts.Value;

    /// <summary>
    /// Counts films per decade, oldest first. Films without a year are left out.
    /// </summary>
    /// <param name="films">The films.</param>
    /// <returns>The decades that have at least one film.</returns>
    public static IReadOnlyList<DecadeCount> CountByDecade(IEnumerable<Film> films)
        => films
            .Where(f => f.Decade is not null)
            .GroupBy(f => f.Decade!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new DecadeCount(g.Key, g.Count()))
            .ToArray();

    /// <summary>
    /// Lists the films that pass every filter that is set, oldest first.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The films, or nothing when the filter names an unknown movement or person.</returns>
    public IEnumerable<Film> Filter(FilmFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IEnumerable<Film> films = Films;

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            films = films.Where(f => f.Countries.Any(c => string.Equals(c.Code, filter.Country, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            PersonRef? person = PersonRef.Parse(filter.Person);
            if (person is null)
            {
                return [];
            }

            PersonRole[] roles = filter.Role is PersonRole role ? [role] : Enum.GetValues<PersonRole>();
            films = films.Where(f => roles.Any(r => f.GetCredits(r).Any(person.Matches)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Movement))
        {
            MovementMatcher? matcher = _movements.FirstOrDefault(m => string.Equals(m.Movement.Id, filter.Movement, StringComparison.OrdinalIgnoreCase));
            if (matcher is null)
            {
                return [];
            }

            films = films.Where(matcher.Matches);
        }

        if (filter.Decade is int decade)
        {
            films = films.Where(f => f.Decade == decade);
        }

        return films
            .OrderBy(f => f.Year ?? int.MaxValue)
            .ThenBy(f => f.SortName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Picks the name to show for a person credited under several spellings.
    /// </summary>
    /// <param name="names">Every credited spelling, one entry per credit.</param>
    /// <returns>The most frequent spelling, preferring the Latin script on a tie.</returns>
    internal static string ChooseName(IEnumerable<string> names)
        => names
            .GroupBy(n => n, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Any(char.IsAsciiLetter))
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .First().Key;

    private static Film[] ShareTmdbIds(Film[] films)
    {
        IEnumerable<Credit> AllCredits() => films.SelectMany(f => Enum.GetValues<PersonRole>().SelectMany(f.GetCredits));

        // A name that leads to two TMDB people is a homonym: leave its credits alone.
        Dictionary<string, string> idByName = AllCredits()
            .Where(c => c.TmdbId is not null && c.NameKey.Length > 0)
            .GroupBy(c => c.NameKey, StringComparer.Ordinal)
            .Select(g => (g.Key, Ids: g.Select(c => c.TmdbId!).Distinct(StringComparer.Ordinal).ToArray()))
            .Where(x => x.Ids.Length == 1)
            .ToDictionary(x => x.Key, x => x.Ids[0], StringComparer.Ordinal);

        if (!AllCredits().Any(c => c.TmdbId is null && idByName.ContainsKey(c.NameKey)))
        {
            return films;
        }

        IReadOnlyList<Credit> Fill(IReadOnlyList<Credit> credits)
            => credits.Select(c => c.TmdbId is null && idByName.TryGetValue(c.NameKey, out string? id) ? c with { TmdbId = id } : c).ToArray();

        return films.Select(f => f.WithCredits(Fill)).ToArray();
    }

    private static IReadOnlyList<Country> MostFrequent(IEnumerable<Country> countries, int count)
        => countries
            .GroupBy(c => c.Code, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.First().Name, StringComparer.Ordinal)
            .Take(count)
            .Select(g => g.First())
            .ToArray();

    // A person credited twice on the same film (co-director listed twice, say) counts once.
    private static IEnumerable<Credit> DistinctPeople(IEnumerable<Credit> credits)
        => credits.Where(c => c.NameKey.Length > 0 || c.TmdbId is not null).DistinctBy(c => c.Key, StringComparer.Ordinal);

    private IReadOnlyList<PersonSummary> Summarize(PersonRole role)
        => Films
            .SelectMany(f => DistinctPeople(f.GetCredits(role)).Select(c => (Credit: c, Film: f)))
            .GroupBy(x => x.Credit.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                int[] years = g.Where(x => x.Film.Year is not null).Select(x => x.Film.Year!.Value).ToArray();
                return new PersonSummary(
                    g.Key,
                    g.First().Credit.TmdbId,
                    ChooseName(g.Select(x => x.Credit.Name)),
                    g.Count(),
                    years.Length > 0 ? years.Min() : null,
                    years.Length > 0 ? years.Max() : null,
                    MostFrequent(g.SelectMany(x => x.Film.Countries), TopCountries));
            })
            .OrderByDescending(p => p.FilmCount)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<CountrySummary> SummarizeCountries()
        => Films
            .SelectMany(f => f.Countries.Select(c => (Country: c, Film: f)))
            .GroupBy(x => x.Country.Code, StringComparer.Ordinal)
            .Select(g => new CountrySummary(
                g.First().Country,
                g.Count(),
                CountByDecade(g.Select(x => x.Film)),
                g.SelectMany(x => DistinctPeople(x.Film.Directors))
                    .GroupBy(c => c.Key, StringComparer.Ordinal)
                    .OrderByDescending(d => d.Count())
                    .ThenBy(d => d.First().Name, StringComparer.OrdinalIgnoreCase)
                    .Take(TopDirectors)
                    .Select(d => new PersonLink(d.Key, ChooseName(d.Select(c => c.Name))))
                    .ToArray()))
            .OrderByDescending(c => c.FilmCount)
            .ThenBy(c => c.Country.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
