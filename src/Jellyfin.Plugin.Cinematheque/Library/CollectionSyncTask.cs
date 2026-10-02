using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Cinematheque.Library;

/// <summary>
/// The scheduled task that keeps movement collections in sync. It also runs after each library
/// scan and after the configuration is saved.
/// </summary>
public class CollectionSyncTask : IScheduledTask
{
    private readonly CollectionSync _collectionSync;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionSyncTask"/> class.
    /// </summary>
    /// <param name="collectionSync">The collection sync.</param>
    public CollectionSyncTask(CollectionSync collectionSync)
    {
        _collectionSync = collectionSync;
    }

    /// <inheritdoc />
    public string Name => "Sync movement collections";

    /// <inheritdoc />
    public string Key => "CinemathequeCollectionSync";

    /// <inheritdoc />
    public string Description => "Creates and updates a collection for each Cinematheque movement, when enabled in the plugin settings.";

    /// <inheritdoc />
    public string Category => "Cinematheque";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => _collectionSync.SyncAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromDays(1).Ticks };
    }
}
