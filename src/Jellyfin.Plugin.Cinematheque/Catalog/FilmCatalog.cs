using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// An in-memory view of one user's films, indexed by director, actor, country and movement.
/// </summary>
/// <remarks>
/// The catalog is immutable once built, so one instance can serve concurrent requests. People are
/// grouped by <see cref="Names.Key"/>, which merges spelling variants of the same name; the
/// spelling shown is the one credited most often.
/// </remarks>
public sealed class FilmCatalog
{
    private const int TopCountries = 3;
    private const int TopDirectors = 5;

    private readonly Lazy<IReadOnlyList<PersonSummary>> _directors;
    private readonly Lazy<IReadOnlyList<PersonSummary>> _actors;
    private readonly Lazy<IReadOnlyList<CountrySummary>> _countries;
    private readonly IReadOnlyList<MovementMatcher> _movements;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilmCatalog"/> class.
    /// </summary>
    /// <param name="films">The films visible to the user.</param>
    /// <param name="movements">The configured movements.</param>
    public FilmCatalog(IEnumerable<Film> films, IEnumerable<MovementDefinition> movements)
    {
        ArgumentNullException.ThrowIfNull(films);
        ArgumentNullException.ThrowIfNull(movements);

        Films = films.ToArray();
        _movements = movements
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => new MovementMatcher(m))
            .ToArray();
        _directors = new Lazy<IReadOnlyList<PersonSummary>>(() => Summarize(f => f.Directors));
        _actors = new Lazy<IReadOnlyList<PersonSummary>>(() => Summarize(f => f.Actors));
        _countries = new Lazy<IReadOnlyList<CountrySummary>>(SummarizeCountries);
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
    public IReadOnlyList<PersonSummary> GetPeople(PersonRole role)
        => role == PersonRole.Director ? _directors.Value : _actors.Value;

    /// <summary>
    /// Lists the production countries, most represented first.
    /// </summary>
    /// <returns>The countries.</returns>
    public IReadOnlyList<CountrySummary> GetCountries() => _countries.Value;

    /// <summary>
    /// Lists the configured movements with the number of films each one matches.
    /// </summary>
    /// <returns>The movements, in configuration order.</returns>
    public IReadOnlyList<(MovementDefinition Movement, int FilmCount)> GetMovements()
        => _movements.Select(m => (m.Movement, Films.Count(m.Matches))).ToArray();

    /// <summary>
    /// Lists the films that pass every filter that is set, oldest first.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The films, or nothing when the filter names an unknown movement.</returns>
    public IEnumerable<Film> Filter(FilmFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IEnumerable<Film> films = Films;

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            films = films.Where(f => f.Countries.Any(c => string.Equals(c.Code, filter.Country, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Director))
        {
            string key = Names.Key(filter.Director);
            films = films.Where(f => f.Directors.Any(d => Names.Key(d) == key));
        }

        if (!string.IsNullOrWhiteSpace(filter.Actor))
        {
            string key = Names.Key(filter.Actor);
            films = films.Where(f => f.Actors.Any(a => Names.Key(a) == key));
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

    private static IReadOnlyList<Country> MostFrequent(IEnumerable<Country> countries, int count)
        => countries
            .GroupBy(c => c.Code, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.First().Name, StringComparer.Ordinal)
            .Take(count)
            .Select(g => g.First())
            .ToArray();

    private IReadOnlyList<PersonSummary> Summarize(Func<Film, IReadOnlyList<string>> credits)
    {
        Dictionary<string, List<(string Name, Film Film)>> byPerson = new Dictionary<string, List<(string Name, Film Film)>>(StringComparer.Ordinal);
        foreach (Film film in Films)
        {
            // A person credited twice on the same film (co-director listed twice, say) counts once.
            foreach (string name in credits(film).DistinctBy(Names.Key))
            {
                string key = Names.Key(name);
                if (key.Length == 0)
                {
                    continue;
                }

                if (!byPerson.TryGetValue(key, out List<(string Name, Film Film)>? entries))
                {
                    entries = [];
                    byPerson[key] = entries;
                }

                entries.Add((name, film));
            }
        }

        return byPerson.Values
            .Select(entries =>
            {
                int[] years = entries.Where(e => e.Film.Year is not null).Select(e => e.Film.Year!.Value).ToArray();
                string name = entries
                    .GroupBy(e => e.Name, StringComparer.Ordinal)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key, StringComparer.Ordinal)
                    .First().Key;
                return new PersonSummary(
                    name,
                    entries.Count,
                    years.Length > 0 ? years.Min() : null,
                    years.Length > 0 ? years.Max() : null,
                    MostFrequent(entries.SelectMany(e => e.Film.Countries), TopCountries));
            })
            .OrderByDescending(p => p.FilmCount)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<CountrySummary> SummarizeCountries()
        => Films
            .SelectMany(f => f.Countries.Select(c => (Country: c, Film: f)))
            .GroupBy(x => x.Country.Code, StringComparer.Ordinal)
            .Select(g => new CountrySummary(
                g.First().Country,
                g.Count(),
                g.Where(x => x.Film.Decade is not null)
                    .GroupBy(x => x.Film.Decade!.Value)
                    .OrderBy(d => d.Key)
                    .Select(d => new DecadeCount(d.Key, d.Count()))
                    .ToArray(),
                g.SelectMany(x => x.Film.Directors.DistinctBy(Names.Key))
                    .GroupBy(Names.Key, StringComparer.Ordinal)
                    .Where(d => d.Key.Length > 0)
                    .OrderByDescending(d => d.Count())
                    .ThenBy(d => d.First(), StringComparer.OrdinalIgnoreCase)
                    .Take(TopDirectors)
                    .Select(d => d.First())
                    .ToArray()))
            .OrderByDescending(c => c.FilmCount)
            .ThenBy(c => c.Country.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
