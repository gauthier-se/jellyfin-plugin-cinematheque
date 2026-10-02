using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Builds and caches one <see cref="FilmCatalog"/> per user.
/// </summary>
/// <remarks>
/// The catalog is built from the films the user can see, so library access and parental controls
/// apply as they do everywhere else in Jellyfin. Any library or configuration change drops every
/// cached catalog; the next request rebuilds it. A library scan raises many changes in a row, which
/// is cheap: dropping the cache costs nothing and nobody rebuilds until someone browses.
/// </remarks>
public sealed class CatalogProvider : IDisposable
{
    // GetPeopleByItems turns the id list into one SQL IN clause, so keep it well under SQLite's
    // parameter limit.
    private const int PeopleBatchSize = 500;

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CatalogProvider> _logger;
    private readonly ConcurrentDictionary<Guid, Lazy<FilmCatalog>> _catalogs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public CatalogProvider(ILibraryManager libraryManager, ILogger<CatalogProvider> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;

        _libraryManager.ItemAdded += OnLibraryChanged;
        _libraryManager.ItemUpdated += OnLibraryChanged;
        _libraryManager.ItemRemoved += OnLibraryChanged;
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

        // Lazy makes concurrent first requests for the same user share one build.
        return _catalogs
            .GetOrAdd(user.Id, _ => new Lazy<FilmCatalog>(() => Build(user), LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemUpdated -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
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
        _logger.LogInformation(
            "Built the Cinematheque catalog for {UserName}: {FilmCount} films in {ElapsedMilliseconds} ms",
            user.Username,
            films.Count,
            stopwatch.ElapsedMilliseconds);
        return catalog;
    }

    private void OnLibraryChanged(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item is MediaBrowser.Controller.Entities.Movies.Movie or Person)
        {
            _catalogs.Clear();
        }
    }

    private void OnConfigurationChanged(object? sender, MediaBrowser.Model.Plugins.BasePluginConfiguration e)
        => _catalogs.Clear();
}
