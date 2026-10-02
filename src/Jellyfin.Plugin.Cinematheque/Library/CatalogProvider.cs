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
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Builds and caches one <see cref="FilmCatalog"/> per user.
/// </summary>
/// <remarks>
/// The catalog is built from the films the user can see, so library access and parental controls
/// apply as they do everywhere else in Jellyfin.
/// <list type="bullet">
/// <item>A change to a user (library access, parental rating) drops that user's catalog at once,
/// so a revoked library never lingers.</item>
/// <item>A configuration change drops every catalog at once.</item>
/// <item>A library change marks every catalog stale. A library scan raises thousands of changes,
/// so a stale catalog keeps being served until it is <see cref="MinRebuildInterval"/> old: at
/// most one rebuild per user per interval while a scan runs.</item>
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
    private readonly ILogger<CatalogProvider> _logger;
    private readonly ConcurrentDictionary<Guid, Entry> _catalogs = new();
    private long _libraryGeneration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    public CatalogProvider(ILibraryManager libraryManager, IUserManager userManager, ILogger<CatalogProvider> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;

        _libraryManager.ItemAdded += OnLibraryChanged;
        _libraryManager.ItemUpdated += OnLibraryChanged;
        _libraryManager.ItemRemoved += OnLibraryChanged;
        _userManager.OnUserUpdated += OnUserUpdated;
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

        long generation = Interlocked.Read(ref _libraryGeneration);
        Entry entry = _catalogs.AddOrUpdate(
            user.Id,
            _ => new Entry(() => Build(user), generation),
            (_, existing) => existing.IsUsable(generation) ? existing : new Entry(() => Build(user), generation));

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

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemUpdated -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
        _userManager.OnUserUpdated -= OnUserUpdated;
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged -= OnConfigurationChanged;
        }
    }

    private FilmCatalog Build(User user)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PluginConfiguration configuration = Configuration;

        IReadOnlyList<BaseItem> items = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsVirtualItem = false,
            DtoOptions = new DtoOptions(false),
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
        List<Film> films = new List<Film>(items.Count);
        foreach (BaseItem item in items)
        {
            IReadOnlyList<PersonInfo> credits = people.GetValueOrDefault(item.Id) ?? [];
            films.Add(new Film(
                item.Id,
                item.Name ?? string.Empty,
                item.SortName ?? item.Name ?? string.Empty,
                item.ProductionYear,
                Country.FromLocations(item.ProductionLocations),
                item.Genres ?? [],
                item.Tags ?? [],
                item.GetProviderId(MetadataProvider.Tmdb),
                credits.Where(p => p.Type == PersonKind.Director).Select(p => p.Name).ToArray(),
                credits.Where(p => p.Type == PersonKind.Actor).Select(p => p.Name).Take(actorsPerFilm).ToArray()));
        }

        FilmCatalog catalog = new FilmCatalog(films, configuration.Movements ?? []);
        _logger.LogDebug(
            "Built the Cinematheque catalog for user {UserId}: {FilmCount} films in {ElapsedMilliseconds} ms",
            user.Id,
            films.Count,
            stopwatch.ElapsedMilliseconds);
        return catalog;
    }

    private void OnLibraryChanged(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item is Movie)
        {
            Interlocked.Increment(ref _libraryGeneration);
        }
    }

    private void OnUserUpdated(object? sender, GenericEventArgs<User> e)
        => _catalogs.TryRemove(e.Argument.Id, out _);

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
        => _catalogs.Clear();

    private sealed class Entry
    {
        private readonly long _createdAt = Environment.TickCount64;

        public Entry(Func<FilmCatalog> build, long generation)
        {
            Catalog = new Lazy<FilmCatalog>(build, LazyThreadSafetyMode.ExecutionAndPublication);
            Generation = generation;
        }

        public Lazy<FilmCatalog> Catalog { get; }

        public long Generation { get; }

        public bool IsUsable(long currentGeneration)
            => Generation == currentGeneration
                || Environment.TickCount64 - _createdAt < (long)MinRebuildInterval.TotalMilliseconds;
    }
}
