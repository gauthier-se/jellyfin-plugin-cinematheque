using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Builds and caches one <see cref="FilmCatalog"/> per user.
/// </summary>
/// <remarks>
/// The server's films are read once, with their credits, and shared by every catalog. Each user's
/// catalog keeps the ones Jellyfin lets that user see, so library access and parental controls
/// apply as they do everywhere else in Jellyfin.
/// <list type="bullet">
/// <item>A change to a user (library access, parental rating) drops that user's catalog at once,
/// so a revoked library never lingers.</item>
/// <item>A configuration change drops every catalog and the shared films at once.</item>
/// <item>A library change marks the shared films stale. A library scan raises thousands of
/// changes, so stale films keep being served until they are <see cref="MinRebuildInterval"/> old:
/// at most one read of the library per interval while a scan runs. Catalogs follow the films they
/// were built from, and are rebuilt on their user's next request.</item>
/// </list>
/// </remarks>
public sealed class CatalogProvider : IDisposable
{
    // GetPeopleByItems turns the id list into one SQL IN clause, so keep it well under SQLite's
    // parameter limit.
    private const int PeopleBatchSize = 500;

    private static readonly TimeSpan MinRebuildInterval = TimeSpan.FromSeconds(30);

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<CatalogProvider> _logger;
    private readonly ConcurrentDictionary<Guid, CatalogEntry> _catalogs = new();
    private readonly ConcurrentDictionary<Guid, Lazy<IReadOnlySet<Guid>>> _seen = new();

    // Credited name to TMDB person id (null when the person has none), shared by every user.
    private readonly ConcurrentDictionary<string, string?> _personTmdbIds = new(StringComparer.Ordinal);
    private readonly Lock _filmsLock = new();
    private FilmsEntry? _films;
    private long _libraryGeneration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="userDataManager">The user data manager.</param>
    /// <param name="logger">The logger.</param>
    public CatalogProvider(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger<CatalogProvider> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;

        _libraryManager.ItemAdded += OnLibraryChanged;
        _libraryManager.ItemUpdated += OnLibraryChanged;
        _libraryManager.ItemRemoved += OnLibraryChanged;
        _userManager.OnUserUpdated += OnUserUpdated;
        _userDataManager.UserDataSaved += OnUserDataSaved;
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged += OnConfigurationChanged;
        }
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>
    /// Gets the catalog of the films a user can see.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The catalog.</returns>
    public FilmCatalog GetCatalog(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        FilmsEntry films = CurrentFilms();
        CatalogEntry entry = _catalogs.AddOrUpdate(
            user.Id,
            _ => new CatalogEntry(films, () => BuildCatalog(user, films)),
            (_, existing) => existing.Films == films ? existing : new CatalogEntry(films, () => BuildCatalog(user, films)));

        try
        {
            return entry.Catalog.Value;
        }
        catch
        {
            // Lazy caches exceptions: forget the failed build so the next request retries.
            _catalogs.TryRemove(KeyValuePair.Create(user.Id, entry));
            throw;
        }
    }

    /// <summary>
    /// Gets the films a user has watched.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The ids of the watched films.</returns>
    public IReadOnlySet<Guid> GetSeen(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        Lazy<IReadOnlySet<Guid>> seen = _seen.GetOrAdd(
            user.Id,
            _ => new Lazy<IReadOnlySet<Guid>>(() => LoadSeen(user), LazyThreadSafetyMode.ExecutionAndPublication));
        try
        {
            return seen.Value;
        }
        catch
        {
            _seen.TryRemove(KeyValuePair.Create(user.Id, seen));
            throw;
        }
    }

    /// <summary>
    /// Builds a catalog of every film on the server, regardless of user access. Read afresh rather
    /// than from the shared films: it feeds background work such as collection sync, which runs
    /// right after a library scan and must not miss its last films.
    /// </summary>
    /// <returns>The catalog.</returns>
    public FilmCatalog BuildServerCatalog() => new FilmCatalog(LoadFilms(), Configuration.GetEffectiveMovements());

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemUpdated -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
        _userManager.OnUserUpdated -= OnUserUpdated;
        _userDataManager.UserDataSaved -= OnUserDataSaved;
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged -= OnConfigurationChanged;
        }
    }

    private HashSet<Guid> LoadSeen(User user)
        => _libraryManager.GetItemIds(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsVirtualItem = false,
            IsPlayed = true,
        }).ToHashSet();

    // The shared films, read again once the library changed and the interval has passed.
    private FilmsEntry CurrentFilms()
    {
        long generation = Interlocked.Read(ref _libraryGeneration);
        lock (_filmsLock)
        {
            if (_films is null || !_films.IsUsable(generation))
            {
                _films = new FilmsEntry(LoadFilms, generation);

                // Catalogs of users who are not around would keep the old films in memory.
                _catalogs.Clear();
            }

            return _films;
        }
    }

    private IReadOnlyList<Film> GetFilms(FilmsEntry entry)
    {
        try
        {
            return entry.Films.Value;
        }
        catch
        {
            // Lazy caches exceptions: forget the failed read so the next request retries.
            lock (_filmsLock)
            {
                if (_films == entry)
                {
                    _films = null;
                }
            }

            throw;
        }
    }

    private FilmCatalog BuildCatalog(User user, FilmsEntry films)
    {
        HashSet<Guid> visible = _libraryManager.GetItemIds(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsVirtualItem = false,
        }).ToHashSet();

        return new FilmCatalog(GetFilms(films).Where(f => visible.Contains(f.Id)), Configuration.GetEffectiveMovements());
    }

    // Every film on the server with its credits, whoever can see it.
    private List<Film> LoadFilms()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PluginConfiguration configuration = Configuration;

        // Provider ids are a joined table that Jellyfin only loads when asked; images and user
        // data are left out, since every joined table multiplies the rows returned.
        DtoOptions options = DtoOptions.StoredColumnsOnly;
        options.Fields = [ItemFields.ProviderIds];
        IReadOnlyList<BaseItem> items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsVirtualItem = false,
            DtoOptions = options,
        });

        Dictionary<Guid, IReadOnlyList<PersonInfo>> people = new Dictionary<Guid, IReadOnlyList<PersonInfo>>(items.Count);
        foreach (Guid[] batch in items.Select(i => i.Id).Chunk(PeopleBatchSize))
        {
            foreach ((Guid itemId, IReadOnlyList<PersonInfo> credits) in _libraryManager.GetPeopleByItems(batch))
            {
                people[itemId] = credits;
            }
        }

        int actorsPerFilm = Math.Max(1, configuration.ActorsPerFilm);
        string[] CreditedNames(BaseItem item, PersonKind kind, int max = int.MaxValue)
            => (people.GetValueOrDefault(item.Id) ?? [])
                .Where(p => p.Type == kind && !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => p.Name)
                .Take(max)
                .ToArray();

        var credited = items
            .Select(item => (
                Item: item,
                Directors: CreditedNames(item, PersonKind.Director),
                Actors: CreditedNames(item, PersonKind.Actor, actorsPerFilm),
                Writers: CreditedNames(item, PersonKind.Writer)))
            .ToArray();
        IReadOnlyDictionary<string, string> tmdbIds = ResolvePersonTmdbIds(
            credited.SelectMany(c => c.Directors.Concat(c.Actors).Concat(c.Writers)).Distinct(StringComparer.Ordinal));
        Credit[] Credits(string[] names) => names.Select(n => new Credit(n, tmdbIds.GetValueOrDefault(n))).ToArray();

        List<Film> films = new List<Film>(items.Count);
        foreach ((BaseItem item, string[] directors, string[] actors, string[] writers) in credited)
        {
            films.Add(new Film(
                item.Id,
                item.Name ?? string.Empty,
                item.SortName ?? item.Name ?? string.Empty,
                item.ProductionYear,
                Country.FromLocations(item.ProductionLocations),
                item.Genres ?? [],
                item.Tags ?? [],
                item.GetProviderId(MetadataProvider.Tmdb),
                Credits(directors),
                Credits(actors),
                Credits(writers)));
        }

        _logger.LogDebug("Read {FilmCount} films for Cinematheque in {ElapsedMilliseconds} ms", films.Count, stopwatch.ElapsedMilliseconds);
        return films;
    }

    /// <summary>
    /// Looks up the TMDB ids of credited people, from cache when possible.
    /// </summary>
    /// <param name="names">The credited names.</param>
    /// <returns>The TMDB id of every name that has one.</returns>
    private Dictionary<string, string> ResolvePersonTmdbIds(IEnumerable<string> names)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);

        // Person items are keyed by a hash of their name, so the missing ones need no search.
        Dictionary<Guid, List<string>> missing = [];
        foreach (string name in names)
        {
            if (_personTmdbIds.TryGetValue(name, out string? cached))
            {
                if (cached is not null)
                {
                    result[name] = cached;
                }

                continue;
            }

            Guid id = _libraryManager.GetPersonId(name);
            if (!missing.TryGetValue(id, out List<string>? sameId))
            {
                sameId = [];
                missing[id] = sameId;
            }

            sameId.Add(name);
        }

        DtoOptions options = DtoOptions.StoredColumnsOnly;
        options.Fields = [ItemFields.ProviderIds];
        foreach (Guid[] batch in missing.Keys.Chunk(PeopleBatchSize))
        {
            foreach (BaseItem person in _libraryManager.GetItemList(new InternalItemsQuery
            {
                ItemIds = batch,
                IncludeItemTypes = [BaseItemKind.Person],
                DtoOptions = options,
            }))
            {
                string? tmdbId = person.GetProviderId(MetadataProvider.Tmdb);
                foreach (string name in missing.GetValueOrDefault(person.Id) ?? [])
                {
                    _personTmdbIds[name] = tmdbId;
                    if (tmdbId is not null)
                    {
                        result[name] = tmdbId;
                    }
                }
            }
        }

        // Remember people without an item too, until Jellyfin creates or updates one.
        foreach (string name in missing.Values.SelectMany(n => n))
        {
            _personTmdbIds.TryAdd(name, null);
        }

        return result;
    }

    private void OnLibraryChanged(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item is Person person)
        {
            _personTmdbIds.TryRemove(person.Name, out _);
            Interlocked.Increment(ref _libraryGeneration);
        }
        else if (e.Item is Movie)
        {
            Interlocked.Increment(ref _libraryGeneration);
        }
    }

    private void OnUserUpdated(object? sender, GenericEventArgs<User> e)
    {
        _catalogs.TryRemove(e.Argument.Id, out _);
        _seen.TryRemove(e.Argument.Id, out _);
    }

    // Saved every few seconds during playback; only some reasons can flip the played flag.
    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (e.Item is Movie && e.SaveReason is not (UserDataSaveReason.PlaybackProgress or UserDataSaveReason.PlaybackStart))
        {
            _seen.TryRemove(e.UserId, out _);
        }
    }

    // Movements change every catalog; actors per film change the films themselves.
    private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
    {
        lock (_filmsLock)
        {
            _films = null;
        }

        _catalogs.Clear();
    }

    private sealed class FilmsEntry
    {
        private readonly long _createdAt = Environment.TickCount64;

        public FilmsEntry(Func<IReadOnlyList<Film>> load, long generation)
        {
            Films = new Lazy<IReadOnlyList<Film>>(load, LazyThreadSafetyMode.ExecutionAndPublication);
            Generation = generation;
        }

        public Lazy<IReadOnlyList<Film>> Films { get; }

        public long Generation { get; }

        public bool IsUsable(long currentGeneration)
            => Generation == currentGeneration
                || Environment.TickCount64 - _createdAt < (long)MinRebuildInterval.TotalMilliseconds;
    }

    // A user's catalog, and the shared films it was built from: new films mean a new catalog.
    private sealed class CatalogEntry
    {
        public CatalogEntry(FilmsEntry films, Func<FilmCatalog> build)
        {
            Films = films;
            Catalog = new Lazy<FilmCatalog>(build, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public FilmsEntry Films { get; }

        public Lazy<FilmCatalog> Catalog { get; }
    }
}
