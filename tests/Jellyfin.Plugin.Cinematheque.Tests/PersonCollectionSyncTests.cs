using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using Jellyfin.Plugin.Cinematheque.Library;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Cinematheque.Tests;

[Collection(nameof(PluginInstanceCollection))]
public sealed class PersonCollectionSyncTests : IDisposable
{
    // Movement collections stay off: people do not depend on them.
    private readonly FakeServer _server = new(new PluginConfiguration { ActorsPerFilm = 1 });
    private readonly ICollectionManager _collections = Substitute.For<ICollectionManager>();
    private readonly IProviderManager _providers = Substitute.For<IProviderManager>();
    private readonly CatalogProvider _catalogs;
    private readonly CollectionSync _sync;
    private readonly Movie _dragonInn;
    private readonly Movie _aTouchOfZen;

    public PersonCollectionSyncTests()
    {
        _dragonInn = _server.AddFilm("Dragon Inn", directors: ["King Hu"], actors: ["Shih Chun", "Polly Shang-Kuan"]);
        _aTouchOfZen = _server.AddFilm("A Touch of Zen", directors: ["King Hu"], actors: ["Hsu Feng", "Shih Chun"]);
        _server.AddFilm("Come Drink with Me", directors: ["King Hu"], actors: ["Cheng Pei-pei"]);
        _server.AddFilm("The Young Master", directors: ["Jackie Chan"], actors: ["Jackie Chan"]);
        _collections.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>()).Returns(call =>
        {
            CollectionCreationOptions options = call.Arg<CollectionCreationOptions>();
            BoxSet collection = new() { Id = Guid.NewGuid(), Name = options.Name, IsLocked = options.IsLocked };
            _server.Add(collection);
            return collection;
        });

        IServerConfigurationManager serverConfiguration = Substitute.For<IServerConfigurationManager>();
        serverConfiguration.Configuration.Returns(new ServerConfiguration());
        _catalogs = _server.CatalogProvider();
        _sync = new CollectionSync(
            _catalogs,
            _server.Library,
            _collections,
            serverConfiguration,
            _providers,
            Substitute.For<IImageEncoder>(),
            NullLogger<CollectionSync>.Instance);
    }

    public void Dispose()
    {
        _sync.Dispose();
        _catalogs.Dispose();
    }

    [Fact]
    public async Task A_picked_director_gets_a_collection_of_their_films()
    {
        await AddAsync(PersonRole.Director, "King Hu");

        CollectionCreationOptions options = CreatedCollection();
        Assert.Equal("Directed by King Hu", options.Name);
        Assert.True(options.IsLocked);
        Assert.Equal(3, options.ItemIdList.Count);
        Assert.Equal(["person:director:name:kinghu"], _server.Plugin.Configuration.ManagedCollections.Select(c => c.Key));
    }

    [Fact]
    public async Task An_actor_collection_only_counts_the_top_of_the_bill()
    {
        await AddAsync(PersonRole.Actor, "Shih Chun");

        Assert.Equal([_dragonInn.Id.ToString("N")], CreatedCollection().ItemIdList);
        Assert.Equal("Starring Shih Chun", CreatedCollection().Name);
    }

    [Fact]
    public async Task Adding_the_same_person_twice_keeps_one_entry_and_one_collection()
    {
        await AddAsync(PersonRole.Director, "King Hu");
        await AddAsync(PersonRole.Director, "King Hu");

        Assert.Single(_server.Plugin.Configuration.CollectedPeople);
        await _collections.Received(1).CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
    }

    [Fact]
    public async Task The_same_person_in_another_role_is_another_collection()
    {
        await AddAsync(PersonRole.Director, "Jackie Chan");
        await AddAsync(PersonRole.Actor, "Jackie Chan");

        Assert.Equal(
            ["person:actor:name:jackiechan", "person:director:name:jackiechan"],
            _server.Plugin.Configuration.ManagedCollections.Select(c => c.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_scheduled_sync_follows_people_with_movement_collections_off()
    {
        await AddAsync(PersonRole.Director, "King Hu");
        Movie legend = _server.AddFilm("Legend of the Mountain", directors: ["King Hu"]);

        await _sync.SyncAsync(new Progress<double>(), CancellationToken.None);

        await _collections.Received().AddToCollectionAsync(Arg.Any<Guid>(), Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(legend.Id)));
        await _collections.Received(1).CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
    }

    [Fact]
    public async Task A_collection_with_the_same_name_is_left_alone()
    {
        _server.Add(new BoxSet { Id = Guid.NewGuid(), Name = "Directed by King Hu" });

        CollectedPerson entry = await AddAsync(PersonRole.Director, "King Hu");

        await _collections.DidNotReceive().CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
        Assert.Equal(PersonCollectionStatus.NameTaken, _sync.Describe(entry, Find(PersonRole.Director, "King Hu")).Status);
    }

    [Fact]
    public async Task A_created_collection_is_described_as_synced()
    {
        CollectedPerson entry = await AddAsync(PersonRole.Director, "King Hu");

        PersonCollectionState state = _sync.Describe(entry, Find(PersonRole.Director, "King Hu"));

        Assert.Equal(PersonCollectionStatus.Synced, state.Status);
        Assert.Equal(_server.Plugin.Configuration.ManagedCollections[0].CollectionId, state.CollectionId);
    }

    [Fact]
    public async Task The_portrait_becomes_the_poster()
    {
        _server.AddPerson("King Hu", "/people/king-hu.jpg");

        await AddAsync(PersonRole.Director, "King Hu");

        await _providers.Received().SaveImage(
            Arg.Any<BaseItem>(), "/people/king-hu.jpg", Arg.Any<string>(), ImageType.Primary, Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Removing_a_person_keeps_their_collection_unless_asked()
    {
        CollectedPerson entry = await AddAsync(PersonRole.Director, "King Hu");

        Assert.True(await _sync.RemovePersonAsync(entry, deleteCollection: false, CancellationToken.None));

        Assert.Empty(_server.Plugin.Configuration.CollectedPeople);
        Assert.Empty(_server.Plugin.Configuration.ManagedCollections);
        _server.Library.DidNotReceive().DeleteItem(Arg.Any<BaseItem>(), Arg.Any<DeleteOptions>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Removing_a_person_can_delete_their_collection()
    {
        CollectedPerson entry = await AddAsync(PersonRole.Director, "King Hu");
        Guid collectionId = _server.Plugin.Configuration.ManagedCollections[0].CollectionId;

        Assert.True(await _sync.RemovePersonAsync(entry, deleteCollection: true, CancellationToken.None));

        _server.Library.Received().DeleteItem(Arg.Is<BaseItem>(c => c.Id == collectionId), Arg.Any<DeleteOptions>(), true);
    }

    [Fact]
    public async Task Removing_someone_who_is_not_listed_does_nothing()
    {
        CollectedPerson stranger = CollectedPerson.For(PersonRole.Director, Find(PersonRole.Director, "Jackie Chan")!);

        Assert.False(await _sync.RemovePersonAsync(stranger, deleteCollection: true, CancellationToken.None));
    }

    private PersonSummary? Find(PersonRole role, string name) => _catalogs.BuildServerCatalog().FindPerson(role, name);

    private Task<CollectedPerson> AddAsync(PersonRole role, string name)
        => _sync.AddPersonAsync(role, Find(role, name)!, CancellationToken.None);

    private CollectionCreationOptions CreatedCollection()
        => (CollectionCreationOptions)_collections.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(ICollectionManager.CreateCollectionAsync)).GetArguments()[0]!;
}
