using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class CollectedPersonTests
{
    [Fact]
    public void A_person_added_by_name_is_not_mistaken_for_a_more_prolific_namesake()
    {
        FilmCatalog catalog = Catalog(
            FilmFactory.Film("One", directors: ["John Smith@1"]),
            FilmFactory.Film("Two", directors: ["John Smith@1"]),
            FilmFactory.Film("Three", directors: ["John Smith@2"]),
            FilmFactory.Film("Four", directors: ["John Smith"]));

        Assert.Equal("name:johnsmith", Director("John Smith").Resolve(catalog)?.Key);
    }

    [Fact]
    public void A_person_whose_films_are_gone_does_not_resolve_to_a_namesake()
        => Assert.Null(Director("John Smith (tmdb:1)").Resolve(Catalog(FilmFactory.Film("Three", directors: ["John Smith@2"]))));

    [Fact]
    public void A_person_added_by_name_is_found_once_they_have_a_tmdb_id()
        => Assert.Equal("tmdb:123", Director("King Hu").Resolve(Catalog(FilmFactory.Film("Dragon Inn", directors: ["King Hu@123"])))?.Key);

    [Fact]
    public void A_name_shared_by_two_people_resolves_to_neither()
    {
        FilmCatalog catalog = Catalog(
            FilmFactory.Film("One", directors: ["John Smith@1"]),
            FilmFactory.Film("Three", directors: ["John Smith@2"]));

        Assert.Null(Director("John Smith").Resolve(catalog));
    }

    [Fact]
    public void A_person_resolves_in_their_role_only()
        => Assert.Null(Director("Shih Chun").Resolve(Catalog(FilmFactory.Film("Dragon Inn", actors: ["Shih Chun"]))));

    private static CollectedPerson Director(string person) => new() { Role = PersonRole.Director, Person = person };

    private static FilmCatalog Catalog(params Film[] films) => new(films, []);
}
