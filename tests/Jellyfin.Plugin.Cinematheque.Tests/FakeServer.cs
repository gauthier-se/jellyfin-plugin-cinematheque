using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Cinematheque.Configuration;
using Jellyfin.Plugin.Cinematheque.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Cinematheque.Tests;

/// <summary>
/// A Jellyfin server made of substitutes: a film library, users with their own access, and the
/// plugin installed with a given configuration.
/// </summary>
/// <remarks>
/// It sets <see cref="Plugin.Instance"/> and <see cref="BaseItem.LibraryManager"/>, which are static,
/// so test classes that use it belong to <see cref="PluginInstanceCollection"/> and never run at
/// the same time.
/// </remarks>
internal sealed class FakeServer
{
    private readonly Dictionary<Guid, BaseItem> _items = [];
    private readonly Dictionary<Guid, User[]> _visibleTo = [];
    private readonly Dictionary<Guid, PersonInfo[]> _people = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _played = [];

    public FakeServer(PluginConfiguration? configuration = null)
    {
        IApplicationPaths paths = Substitute.For<IApplicationPaths>();
        paths.PluginsPath.Returns(Path.GetTempPath());
        paths.PluginConfigurationsPath.Returns(Path.GetTempPath());
        XmlSerializer.DeserializeFromFile(typeof(PluginConfiguration), Arg.Any<string>())
            .Returns(configuration ?? new PluginConfiguration());
        Plugin = new Plugin(paths, XmlSerializer, Substitute.For<ITaskManager>());

        // Items save themselves through this static, as in the server.
        BaseItem.LibraryManager = Library;

        Library.GetItemList(Arg.Any<InternalItemsQuery>()).Returns(call => Query(call.Arg<InternalItemsQuery>()));
        Library.GetItemIds(Arg.Any<InternalItemsQuery>()).Returns(call => Query(call.Arg<InternalItemsQuery>()).Select(i => i.Id).ToArray());
        Library.GetItemById(Arg.Any<Guid>()).Returns(call => _items.GetValueOrDefault(call.Arg<Guid>()));
        Library.GetPeopleByItems(Arg.Any<IReadOnlyList<Guid>>()).Returns(call => call.Arg<IReadOnlyList<Guid>>()
            .Where(_people.ContainsKey)
            .ToDictionary(id => id, id => (IReadOnlyList<PersonInfo>)_people[id]));
    }

    public Plugin Plugin { get; }

    public IXmlSerializer XmlSerializer { get; } = Substitute.For<IXmlSerializer>();

    public ILibraryManager Library { get; } = Substitute.For<ILibraryManager>();

    public IUserManager Users { get; } = Substitute.For<IUserManager>();

    public IUserDataManager UserData { get; } = Substitute.For<IUserDataManager>();

    public static User User(string name) => new(name, "Default", "Default");

    public CatalogProvider CatalogProvider() => new(Library, Users, UserData, NullLogger<CatalogProvider>.Instance);

    /// <summary>
    /// Adds a film, visible to every user unless <paramref name="visibleTo"/> names some.
    /// </summary>
    public Movie AddFilm(string name, string? tmdbId = null, string[]? directors = null, string? poster = null, User[]? visibleTo = null)
    {
        Movie film = new() { Id = Guid.NewGuid(), Name = name, SortName = name, ProductionYear = 1970 };
        if (tmdbId is not null)
        {
            film.SetProviderId(MetadataProvider.Tmdb, tmdbId);
        }

        if (poster is not null)
        {
            film.ImageInfos = [new ItemImageInfo { Type = ImageType.Primary, Path = poster }];
        }

        _people[film.Id] = [.. (directors ?? []).Select(d => new PersonInfo { Name = d, Type = PersonKind.Director })];
        Add(film);
        if (visibleTo is not null)
        {
            ShowTo(film, visibleTo);
        }

        return film;
    }

    public void Add(BaseItem item) => _items[item.Id] = item;

    /// <summary>
    /// Changes who can see a film, as a change to library access or parental controls would.
    /// </summary>
    public void ShowTo(Movie film, params User[] users) => _visibleTo[film.Id] = users;

    public void MarkPlayed(User user, Movie film)
    {
        if (!_played.TryGetValue(user.Id, out HashSet<Guid>? played))
        {
            played = [];
            _played[user.Id] = played;
        }

        played.Add(film.Id);
    }

    public void RaiseUserUpdated(User user)
        => Users.OnUserUpdated += Raise.EventWith(Users, new Jellyfin.Data.Events.GenericEventArgs<User>(user));

    // What the database would answer: the query's item types, as seen by the query's user.
    private BaseItem[] Query(InternalItemsQuery query)
    {
        IEnumerable<BaseItem> items = _items.Values;
        if (query.IncludeItemTypes.Length > 0)
        {
            items = items.Where(i => query.IncludeItemTypes.Contains(i.GetBaseItemKind()));
        }

        if (query.ItemIds.Length > 0)
        {
            items = items.Where(i => query.ItemIds.Contains(i.Id));
        }

        if (query.Name is not null)
        {
            items = items.Where(i => i.Name == query.Name);
        }

        if (query.User is User user)
        {
            items = items.Where(i => !_visibleTo.TryGetValue(i.Id, out User[]? users) || users.Any(u => u.Id == user.Id));
            if (query.IsPlayed == true)
            {
                items = items.Where(i => _played.GetValueOrDefault(user.Id)?.Contains(i.Id) ?? false);
            }
        }

        return [.. items];
    }
}

/// <summary>
/// Test classes that set the static <see cref="Plugin.Instance"/>.
/// </summary>
[CollectionDefinition(nameof(PluginInstanceCollection), DisableParallelization = true)]
public sealed class PluginInstanceCollection;
