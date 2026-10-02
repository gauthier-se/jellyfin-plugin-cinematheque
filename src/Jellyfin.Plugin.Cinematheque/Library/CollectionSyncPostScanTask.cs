using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// Syncs movement collections at the end of each library scan, so new films join them at once.
/// </summary>
public class CollectionSyncPostScanTask : ILibraryPostScanTask
{
    private readonly CollectionSync _collectionSync;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionSyncPostScanTask"/> class.
    /// </summary>
    /// <param name="collectionSync">The collection sync.</param>
    public CollectionSyncPostScanTask(CollectionSync collectionSync)
    {
        _collectionSync = collectionSync;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        => _collectionSync.SyncAsync(progress, cancellationToken);
}
