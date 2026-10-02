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
/// Mirrors each movement into a Jellyfin collection, so movements also reach the TV and mobile
/// apps that never load the Cinematheque tab.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Off by default: <see cref="PluginConfiguration.SyncCollections"/>.</item>
/// <item>Only collections the plugin created are touched, tracked in
/// <see cref="PluginConfiguration.ManagedCollections"/>. Their contents follow the movement's
/// rules, so films added to them by hand are removed on the next sync.</item>
/// <item>Nothing is ever deleted. A hidden or removed movement keeps its collection as it was.</item>
/// <item>An existing collection with the same name is left alone and the movement is skipped.</item>
/// <item>Collections are locked, which keeps internet providers from renaming them.</item>
/// <item>A collection without a poster gets one: four posters of its films in a grid, or the first
/// film's poster when it has fewer. A poster set by hand is never replaced.</item>
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

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();

    /// <summary>
    /// Creates and updates the movement collections, when the option is on.
    /// </summary>
    /// <param name="progress">The progress, from 0 to 100.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the collections are in sync.</returns>
    public async Task SyncAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        Plugin? plugin = Plugin.Instance;
        if (plugin is null || !plugin.Configuration.SyncCollections)
        {
            return;
        }

        // A library scan and the scheduled task can both ask at once.
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SyncLockedAsync(plugin, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SyncLockedAsync(Plugin plugin, IProgress<double> progress, CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = plugin.Configuration;
        string language = _serverConfigurationManager.Configuration.UICulture;
        Dictionary<string, Guid> managed = (configuration.ManagedCollections ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.MovementId))
            .GroupBy(c => c.MovementId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().CollectionId, StringComparer.OrdinalIgnoreCase);
        bool created = false;

        IReadOnlyList<MovementSummary> movements = _catalogProvider.BuildServerCatalog().GetMovements();
        for (int i = 0; i < movements.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MovementSummary summary = movements[i];
            string movementId = summary.Movement.Id;
            BoxSet? collection = managed.TryGetValue(movementId, out Guid collectionId)
                ? _libraryManager.GetItemById(collectionId) as BoxSet
                : null;

            if (collection is null)
            {
                collection = await CreateAsync(summary, language).ConfigureAwait(false);
                if (collection is not null)
                {
                    managed[movementId] = collection.Id;
                    created = true;
                }
            }
            else
            {
                await UpdateAsync(collection, summary).ConfigureAwait(false);
            }

            if (collection is not null)
            {
                await EnsurePosterAsync(collection, summary, cancellationToken).ConfigureAwait(false);
            }

            progress.Report(100.0 * (i + 1) / movements.Count);
        }

        if (created)
        {
            configuration.ManagedCollections = managed
                .Select(m => new MovementCollectionLink { MovementId = m.Key, CollectionId = m.Value })
                .ToArray();
            plugin.SaveConfiguration();
        }
    }

    private async Task<BoxSet?> CreateAsync(MovementSummary summary, string language)
    {
        if (summary.FilmCount == 0)
        {
            return null;
        }

        string name = summary.Movement.Localize(language).Name;
        bool nameTaken = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            Recursive = true,
            Name = name,
            DtoOptions = DtoOptions.StoredColumnsOnly,
        }).Count > 0;
        if (nameTaken)
        {
            _logger.LogWarning(
                "A collection named {CollectionName} already exists, so Cinematheque leaves it alone and does not sync the {MovementId} movement",
                name,
                summary.Movement.Id);
            return null;
        }

        BoxSet collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
        {
            Name = name,
            IsLocked = true,
            ItemIdList = summary.FilmIds.Select(id => id.ToString("N")).ToArray(),
        }).ConfigureAwait(false);
        _logger.LogInformation("Created the {CollectionName} collection with {FilmCount} films", name, summary.FilmCount);
        return collection;
    }

    private async Task UpdateAsync(BoxSet collection, MovementSummary summary)
    {
        // Collections created before 0.2 were not locked.
        if (!collection.IsLocked)
        {
            collection.IsLocked = true;
            await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        HashSet<Guid> wanted = summary.FilmIds.ToHashSet();
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

    private async Task EnsurePosterAsync(BoxSet collection, MovementSummary summary, CancellationToken cancellationToken)
    {
        if (collection.HasImage(ImageType.Primary))
        {
            return;
        }

        string[] posters = summary.FilmIds
            .Select(id => _libraryManager.GetItemById(id))
            .Where(film => film is not null && film.HasImage(ImageType.Primary) && film.GetImageInfo(ImageType.Primary, 0).IsLocalFile)
            .Select(film => film!.GetImagePath(ImageType.Primary))
            .Take(4)
            .ToArray();
        if (posters.Length == 0)
        {
            return;
        }

        // Fewer than four posters would repeat in the grid; one poster alone looks better.
        string output = Path.Combine(_serverConfigurationManager.ApplicationPaths.TempDirectory, "cinematheque-" + collection.Id.ToString("N") + ".png");
        string source = posters[0];
        if (posters.Length == 4 && _imageEncoder.SupportsImageCollageCreation)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            _imageEncoder.CreateImageCollage(new ImageCollageOptions { InputPaths = posters, OutputPath = output, Width = 600, Height = 900 }, null);
            source = output;
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
}
