using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Mirrors movements, and the people the administrator picked, into Jellyfin collections, so
/// they also reach the TV and mobile apps that never load the Cinematheque tab.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Movements are off by default: <see cref="PluginConfiguration.SyncCollections"/>. People
/// are added one by one: <see cref="PluginConfiguration.CollectedPeople"/>.</item>
/// <item>Only collections the plugin created are touched, tracked in
/// <see cref="PluginConfiguration.ManagedCollections"/>. Their contents follow the movement's
/// rules or the person's films, so films added to them by hand are removed on the next sync.</item>
/// <item>Nothing is deleted, except the collection of a person the administrator removes and
/// asks to delete. A hidden or removed movement keeps its collection as it was.</item>
/// <item>An existing collection with the same name is left alone and its source is skipped.</item>
/// <item>Collections are locked, which keeps internet providers from renaming them.</item>
/// <item>A collection without a poster gets one: the person's portrait, or four posters of its
/// films in a grid, or the first film's poster when it has fewer. A poster set by hand is never
/// replaced.</item>
/// </list>
/// </remarks>
public sealed class CollectionSync : IDisposable
{
    private readonly CatalogProvider _catalogProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IProviderManager _providerManager;
    private readonly IImageEncoder _imageEncoder;
    private readonly ILogger<CollectionSync> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionSync"/> class.
    /// </summary>
    /// <param name="catalogProvider">The catalog provider.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="collectionManager">The collection manager.</param>
    /// <param name="serverConfigurationManager">The server configuration, for its display language.</param>
    /// <param name="providerManager">The provider manager, to save collection posters.</param>
    /// <param name="imageEncoder">The image encoder, to draw collection posters.</param>
    /// <param name="logger">The logger.</param>
    public CollectionSync(
        CatalogProvider catalogProvider,
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        IServerConfigurationManager serverConfigurationManager,
        IProviderManager providerManager,
        IImageEncoder imageEncoder,
        ILogger<CollectionSync> logger)
    {
        _catalogProvider = catalogProvider;
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _serverConfigurationManager = serverConfigurationManager;
        _providerManager = providerManager;
        _imageEncoder = imageEncoder;
        _logger = logger;
    }

    private string Language => _serverConfigurationManager.Configuration.UICulture;

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();

    /// <summary>
    /// Creates and updates the collections of the movements, when the option is on, and of the
    /// people the administrator added.
    /// </summary>
    /// <param name="progress">The progress, from 0 to 100.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the collections are in sync.</returns>
    public async Task SyncAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        Plugin? plugin = Plugin.Instance;
        if (plugin is null || (!plugin.Configuration.SyncCollections && (plugin.Configuration.CollectedPeople ?? []).Length == 0))
        {
            return;
        }

        // A library scan and the scheduled task can both ask at once.
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PluginConfiguration configuration = plugin.Configuration;
            FilmCatalog catalog = _catalogProvider.BuildServerCatalog();
            List<CollectionTarget> targets = [];
            if (configuration.SyncCollections)
            {
                targets.AddRange(catalog.GetMovements().Select(MovementTarget));
            }

            targets.AddRange((configuration.CollectedPeople ?? [])
                .Where(p => p.Identity.Length > 0)
                .Select(p => PersonTarget(p, catalog)));
            await SyncLockedAsync(plugin, targets, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Adds a person to the collections, and creates theirs right away.
    /// </summary>
    /// <param name="role">The role whose films make the collection.</param>
    /// <param name="person">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entry, the one already there when the person was added before.</returns>
    public async Task<CollectedPerson> AddPersonAsync(PersonRole role, PersonSummary person, CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance ?? throw new InvalidOperationException("The plugin is not loaded.");
        CollectedPerson entry = CollectedPerson.For(role, person);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PluginConfiguration configuration = plugin.Configuration;
            CollectedPerson? existing = Array.Find(configuration.CollectedPeople ?? [], p => p.Identity == entry.Identity);
            if (existing is null)
            {
                configuration.CollectedPeople = [.. configuration.CollectedPeople ?? [], entry];
                plugin.SaveConfiguration();
                _logger.LogInformation("Added {PersonName} as {Role} to the Cinematheque collections", entry.Name, role);
            }
            else
            {
                entry = existing;
            }

            // From every film on the server, as the scheduled sync would: the person passed in is
            // what one administrator can see.
            CollectionTarget target = PersonTarget(entry, _catalogProvider.BuildServerCatalog());
            await SyncLockedAsync(plugin, [target], new Progress<double>(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }

        return entry;
    }

    /// <summary>
    /// Removes a person from the collections.
    /// </summary>
    /// <param name="entry">The person, as listed in <see cref="PluginConfiguration.CollectedPeople"/>.</param>
    /// <param name="deleteCollection">Also delete their collection. Otherwise it stays as it is.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>false</c> when the person was not in the list.</returns>
    public async Task<bool> RemovePersonAsync(CollectedPerson entry, bool deleteCollection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Plugin plugin = Plugin.Instance ?? throw new InvalidOperationException("The plugin is not loaded.");
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PluginConfiguration configuration = plugin.Configuration;
            CollectedPerson[] people = configuration.CollectedPeople ?? [];
            if (!people.Any(p => p.Identity == entry.Identity))
            {
                return false;
            }

            MovementCollectionLink? link = Array.Find(configuration.ManagedCollections ?? [], c => c.Key == entry.CollectionKey);
            configuration.CollectedPeople = people.Where(p => p.Identity != entry.Identity).ToArray();
            configuration.ManagedCollections = (configuration.ManagedCollections ?? []).Where(c => c != link).ToArray();
            plugin.SaveConfiguration();

            if (deleteCollection && link is not null && _libraryManager.GetItemById(link.CollectionId) is BoxSet collection)
            {
                _libraryManager.DeleteItem(collection, new DeleteOptions { DeleteFileLocation = true }, true);
                _logger.LogInformation("Deleted the {CollectionName} collection", collection.Name);
            }

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Tells where the collection of a person stands.
    /// </summary>
    /// <param name="entry">The person, as listed in <see cref="PluginConfiguration.CollectedPeople"/>.</param>
    /// <param name="person">The person as found in a catalog, or <c>null</c> when they have no films.</param>
    /// <returns>The state.</returns>
    public PersonCollectionState Describe(CollectedPerson entry, PersonSummary? person)
    {
        ArgumentNullException.ThrowIfNull(entry);

        MovementCollectionLink? link = Array.Find(Plugin.Instance?.Configuration.ManagedCollections ?? [], c => c.Key == entry.CollectionKey);
        if (link is not null && _libraryManager.GetItemById(link.CollectionId) is BoxSet collection)
        {
            return new PersonCollectionState(link.CollectionId, collection.Name, PersonCollectionStatus.Synced);
        }

        string name = entry.CollectionName(person?.Name ?? entry.Name, Language);
        if (person is null || person.FilmCount == 0)
        {
            return new PersonCollectionState(null, name, PersonCollectionStatus.NoFilms);
        }

        return new PersonCollectionState(null, name, IsNameTaken(name) ? PersonCollectionStatus.NameTaken : PersonCollectionStatus.Pending);
    }

    private CollectionTarget MovementTarget(MovementSummary summary)
        => new CollectionTarget(
            MovementCollectionLink.MovementPrefix + summary.Movement.Id,
            summary.Movement.Localize(Language).Name,
            summary.FilmIds,
            null);

    private CollectionTarget PersonTarget(CollectedPerson entry, FilmCatalog catalog)
    {
        PersonSummary? person = entry.Resolve(catalog);
        string name = person?.Name ?? entry.Name;
        return new CollectionTarget(
            entry.CollectionKey,
            entry.CollectionName(name, Language),
            person?.FilmIds ?? [],
            person is null ? null : LocalPoster(_libraryManager.GetPerson(person.Name)));
    }

    private async Task SyncLockedAsync(Plugin plugin, IReadOnlyList<CollectionTarget> targets, IProgress<double> progress, CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> managed = (plugin.Configuration.ManagedCollections ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Key))
            .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().CollectionId, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Guid> created = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CollectionTarget target = targets[i];
                try
                {
                    await SyncTargetAsync(target, managed, created, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One collection failing, on a broken poster say, must not hold back the others.
                    _logger.LogError(ex, "Could not sync the {CollectionKey} collection", target.Key);
                }

                progress.Report(100.0 * (i + 1) / targets.Count);
            }
        }
        finally
        {
            // Saved even when the sync fails or is cancelled halfway: a collection created but not
            // recorded would pass for someone else's on the next sync, and never be synced again.
            if (created.Count > 0)
            {
                SaveManagedCollections(plugin, created);
            }
        }
    }

    // Reads the configuration again rather than reusing the one the sync started with: the
    // administrator may have saved the settings meanwhile, which replaces the object.
    private static void SaveManagedCollections(Plugin plugin, Dictionary<string, Guid> created)
    {
        PluginConfiguration configuration = plugin.Configuration;
        configuration.ManagedCollections = (configuration.ManagedCollections ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Key) && !created.ContainsKey(c.Key))
            .Concat(created.Select(c => new MovementCollectionLink
            {
                Key = c.Key,
                MovementId = c.Key.StartsWith(MovementCollectionLink.MovementPrefix, StringComparison.Ordinal)
                    ? c.Key[MovementCollectionLink.MovementPrefix.Length..]
                    : string.Empty,
                CollectionId = c.Value,
            }))
            .ToArray();
        plugin.SaveConfiguration();
    }

    private async Task SyncTargetAsync(
        CollectionTarget target,
        Dictionary<string, Guid> managed,
        Dictionary<string, Guid> created,
        CancellationToken cancellationToken)
    {
        BoxSet? collection = managed.TryGetValue(target.Key, out Guid collectionId)
            ? _libraryManager.GetItemById(collectionId) as BoxSet
            : null;

        if (collection is null)
        {
            collection = await CreateAsync(target).ConfigureAwait(false);
            if (collection is null)
            {
                return;
            }

            // Recorded before the poster, which is the step most likely to fail.
            created[target.Key] = collection.Id;
        }
        else
        {
            await UpdateAsync(collection, target, cancellationToken).ConfigureAwait(false);
        }

        await EnsurePosterAsync(collection, target, cancellationToken).ConfigureAwait(false);
    }

    private bool IsNameTaken(string name)
        => _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            Recursive = true,
            Name = name,
            DtoOptions = DtoOptions.StoredColumnsOnly,
        }).Count > 0;

    private async Task<BoxSet?> CreateAsync(CollectionTarget target)
    {
        if (target.FilmIds.Count == 0)
        {
            return null;
        }

        if (IsNameTaken(target.Name))
        {
            _logger.LogWarning(
                "A collection named {CollectionName} already exists, so Cinematheque leaves it alone and does not sync {CollectionKey}",
                target.Name,
                target.Key);
            return null;
        }

        BoxSet collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
        {
            Name = target.Name,
            IsLocked = true,
            ItemIdList = target.FilmIds.Select(id => id.ToString("N")).ToArray(),
        }).ConfigureAwait(false);
        _logger.LogInformation("Created the {CollectionName} collection with {FilmCount} films", target.Name, target.FilmIds.Count);
        return collection;
    }

    private async Task UpdateAsync(BoxSet collection, CollectionTarget target, CancellationToken cancellationToken)
    {
        // Collections created before 0.2 were not locked.
        if (!collection.IsLocked)
        {
            collection.IsLocked = true;
            await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        }

        HashSet<Guid> wanted = target.FilmIds.ToHashSet();
        HashSet<Guid> current = collection.GetLinkedChildren(DtoOptions.StoredColumnsOnly).Select(item => item.Id).ToHashSet();

        Guid[] toAdd = wanted.Where(id => !current.Contains(id)).ToArray();
        Guid[] toRemove = current.Where(id => !wanted.Contains(id)).ToArray();
        if (toAdd.Length > 0)
        {
            await _collectionManager.AddToCollectionAsync(collection.Id, toAdd).ConfigureAwait(false);
        }

        if (toRemove.Length > 0)
        {
            await _collectionManager.RemoveFromCollectionAsync(collection.Id, toRemove).ConfigureAwait(false);
        }

        if (toAdd.Length > 0 || toRemove.Length > 0)
        {
            _logger.LogInformation(
                "Synced the {CollectionName} collection: {Added} added, {Removed} removed",
                collection.Name,
                toAdd.Length,
                toRemove.Length);
        }
    }

    private static string? LocalPoster(BaseItem? item)
        => item is not null && item.HasImage(ImageType.Primary) && item.GetImageInfo(ImageType.Primary, 0).IsLocalFile
            ? item.GetImagePath(ImageType.Primary)
            : null;

    private async Task EnsurePosterAsync(BoxSet collection, CollectionTarget target, CancellationToken cancellationToken)
    {
        if (collection.HasImage(ImageType.Primary))
        {
            return;
        }

        string? source = target.Portrait;
        string output = Path.Combine(_serverConfigurationManager.ApplicationPaths.TempDirectory, "cinematheque-" + collection.Id.ToString("N") + ".png");
        if (source is null)
        {
            string[] posters = target.FilmIds
                .Select(id => LocalPoster(_libraryManager.GetItemById(id)))
                .OfType<string>()
                .Take(4)
                .ToArray();
            if (posters.Length == 0)
            {
                return;
            }

            // Fewer than four posters would repeat in the grid; one poster alone looks better.
            source = posters[0];
            if (posters.Length == 4 && _imageEncoder.SupportsImageCollageCreation)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                _imageEncoder.CreateImageCollage(new ImageCollageOptions { InputPaths = posters, OutputPath = output, Width = 600, Height = 900 }, null);
                source = output;
            }
        }

        try
        {
            await _providerManager.SaveImage(collection, source, MimeTypes.GetMimeType(source), ImageType.Primary, null, false, cancellationToken).ConfigureAwait(false);
            await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (source == output)
            {
                File.Delete(output);
            }
        }
    }

    /// <summary>
    /// What a collection mirrors.
    /// </summary>
    /// <param name="Key">The key in <see cref="PluginConfiguration.ManagedCollections"/>.</param>
    /// <param name="Name">The collection name, used when creating it.</param>
    /// <param name="FilmIds">The films it holds.</param>
    /// <param name="Portrait">A portrait to use as its poster, for a person.</param>
    private sealed record CollectionTarget(string Key, string Name, IReadOnlyList<Guid> FilmIds, string? Portrait);
}
