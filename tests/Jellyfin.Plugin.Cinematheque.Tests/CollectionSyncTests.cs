using Jellyfin.Plugin.Cinematheque.Configuration;
using Jellyfin.Plugin.Cinematheque.Library;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Jellyfin.Plugin.Cinematheque.Tests;

[Collection(nameof(PluginInstanceCollection))]
public class CollectionSyncTests
{
    private readonly FakeServer _server = new(new PluginConfiguration
    {
        SyncCollections = true,
        // Only the two movements below, matched by TMDB id.
        HiddenMovements = [.. DefaultMovements.Create().Select(m => m.Id)],
        CustomMovements =
        [
            new MovementDefinition { Id = "wuxia", Name = "Shaw Brothers wuxia", TmdbIds = ["1"] },
            new MovementDefinition { Id = "kung-fu", Name = "Kung fu comedy", TmdbIds = ["2"] },
        ],
    });

    private readonly ICollectionManager _collections = Substitute.For<ICollectionManager>();
    private readonly IProviderManager _providers = Substitute.For<IProviderManager>();

    public CollectionSyncTests()
    {
        _server.AddFilm("Come Drink with Me", tmdbId: "1", poster: "/posters/1.jpg");
        _server.AddFilm("The Young Master", tmdbId: "2", poster: "/posters/2.jpg");
        _collections.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>()).Returns(call =>
        {
            CollectionCreationOptions options = call.Arg<CollectionCreationOptions>();
            BoxSet collection = new() { Id = Guid.NewGuid(), Name = options.Name, IsLocked = options.IsLocked };
            _server.Add(collection);
            return collection;
        });
    }

    [Fact]
    public async Task A_failing_poster_neither_loses_its_collection_nor_stops_the_others()
    {
        PosterFails(new IOException("Disk full"));

        await SyncAsync();

        Assert.Equal(["kung-fu", "wuxia"], ManagedMovements());
    }

    [Fact]
    public async Task Collections_created_before_a_cancellation_are_recorded()
    {
        PosterFails(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(SyncAsync);

        Assert.Equal(["wuxia"], ManagedMovements());
        _server.XmlSerializer.Received().SerializeToFile(_server.Plugin.Configuration, Arg.Any<string>());
    }

    [Fact]
    public async Task A_recorded_collection_is_not_created_again()
    {
        await SyncAsync();
        await SyncAsync();

        await _collections.Received(2).CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
        Assert.Equal(["kung-fu", "wuxia"], ManagedMovements());
    }

    private void PosterFails(Exception exception)
        => _providers.SaveImage(Arg.Any<BaseItem>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ImageType>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

    private async Task SyncAsync()
    {
        IServerConfigurationManager serverConfiguration = Substitute.For<IServerConfigurationManager>();
        serverConfiguration.Configuration.Returns(new ServerConfiguration());
        using CatalogProvider catalogs = _server.CatalogProvider();
        using CollectionSync sync = new(
            catalogs,
            _server.Library,
            _collections,
            serverConfiguration,
            _providers,
            Substitute.For<IImageEncoder>(),
            NullLogger<CollectionSync>.Instance);

        await sync.SyncAsync(new Progress<double>(), CancellationToken.None);
    }

    private string[] ManagedMovements()
        => [.. _server.Plugin.Configuration.ManagedCollections.Select(c => c.MovementId).Order(StringComparer.Ordinal)];
}
