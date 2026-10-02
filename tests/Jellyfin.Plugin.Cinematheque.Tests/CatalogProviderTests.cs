using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Library;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.Cinematheque.Tests;

[Collection(nameof(PluginInstanceCollection))]
public class CatalogProviderTests
{
    private readonly FakeServer _server = new();
    private readonly User _parent = FakeServer.User("parent");
    private readonly User _child = FakeServer.User("child");

    [Fact]
    public void A_catalog_holds_only_the_films_its_user_can_see()
    {
        _server.AddFilm("Tokyo Story", directors: ["Yasujirō Ozu"]);
        _server.AddFilm("In the Realm of the Senses", directors: ["Nagisa Oshima"], visibleTo: [_parent]);
        using CatalogProvider provider = _server.CatalogProvider();

        Assert.Equal(["In the Realm of the Senses", "Tokyo Story"], Titles(provider.GetCatalog(_parent)));
        Assert.Equal(["Tokyo Story"], Titles(provider.GetCatalog(_child)));
        Assert.Equal(["Yasujirō Ozu"], [.. provider.GetCatalog(_child).GetPeople(PersonRole.Director).Select(p => p.Name)]);
    }

    [Fact]
    public void A_catalog_follows_a_change_to_its_user_access()
    {
        Movie film = _server.AddFilm("In the Realm of the Senses", visibleTo: [_parent]);
        using CatalogProvider provider = _server.CatalogProvider();
        Assert.Empty(provider.GetCatalog(_child).Films);

        _server.ShowTo(film, _parent, _child);
        _server.RaiseUserUpdated(_child);

        Assert.Equal(["In the Realm of the Senses"], Titles(provider.GetCatalog(_child)));
    }

    [Fact]
    public void A_revoked_film_leaves_the_catalog_at_once()
    {
        Movie film = _server.AddFilm("In the Realm of the Senses");
        using CatalogProvider provider = _server.CatalogProvider();
        Assert.Single(provider.GetCatalog(_child).Films);

        _server.ShowTo(film, _parent);
        _server.RaiseUserUpdated(_child);

        Assert.Empty(provider.GetCatalog(_child).Films);
        Assert.Single(provider.GetCatalog(_parent).Films);
    }

    [Fact]
    public void Watched_films_belong_to_each_user()
    {
        Movie film = _server.AddFilm("Tokyo Story");
        _server.MarkPlayed(_parent, film);
        using CatalogProvider provider = _server.CatalogProvider();

        Assert.Contains(film.Id, provider.GetSeen(_parent));
        Assert.DoesNotContain(film.Id, provider.GetSeen(_child));
    }

    private static string[] Titles(FilmCatalog catalog) => [.. catalog.Films.Select(f => f.Name).Order(StringComparer.Ordinal)];
}
