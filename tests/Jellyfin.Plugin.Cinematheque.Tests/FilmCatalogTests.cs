using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using static Jellyfin.Plugin.Cinematheque.Tests.FilmFactory;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class FilmCatalogTests
{
    private static readonly Film[] _films =
    [
        Film("A Better Tomorrow", 1986, ["Hong Kong"], ["John Woo"], ["Chow Yun-fat", "Leslie Cheung"], ["Action"]),
        Film("The Killer", 1989, ["Hong Kong"], ["John Woo"], ["Chow Yun-fat", "Danny Lee"], ["Action"]),
        Film("Hard Boiled", 1992, ["Hong Kong"], ["John Woo"], ["Chow Yun-fat", "Tony Leung Chiu-wai"], ["Action"]),
        Film("Chungking Express", 1994, ["Hong Kong"], ["Wong Kar-wai"], ["Tony Leung Chiu-wai", "Faye Wong"]),
        Film("In the Mood for Love", 2000, ["Hong Kong", "France"], ["Wong Kar-wai"], ["Tony Leung Chiu-wai", "Maggie Cheung"]),
        Film("Tokyo Story", 1953, ["Japan"], ["Yasujirō Ozu"], ["Chishū Ryū"]),
        Film("Late Spring", 1949, ["Japan"], ["Yasujiro Ozu"], ["Chishu Ryu"]),
        Film("Face/Off", 1997, ["United States of America"], ["John Woo"], ["John Travolta"]),
    ];

    private static readonly FilmCatalog _catalog = new(_films, DefaultMovements.Create());

    [Fact]
    public void Directors_are_sorted_by_film_count()
    {
        var directors = _catalog.GetPeople(PersonRole.Director);

        Assert.Equal(["John Woo", "Wong Kar-wai", "Yasujiro Ozu"], [.. directors.Select(d => d.Name)]);
        Assert.Equal(4, directors[0].FilmCount);
    }

    [Fact]
    public void Spelling_variants_of_a_name_are_merged()
    {
        PersonSummary ozu = _catalog.GetPeople(PersonRole.Director).Single(d => Names.Key(d.Name) == "yasujiroozu");

        Assert.Equal(2, ozu.FilmCount);
        Assert.Equal(1949, ozu.FirstYear);
        Assert.Equal(1953, ozu.LastYear);
    }

    [Fact]
    public void People_carry_their_main_countries()
    {
        PersonSummary woo = _catalog.GetPeople(PersonRole.Director)[0];

        Assert.Equal(["HK", "US"], [.. woo.Countries.Select(c => c.Code)]);
    }

    [Fact]
    public void Actors_are_counted_across_films()
    {
        var actors = _catalog.GetPeople(PersonRole.Actor);

        Assert.Equal("Chow Yun-fat", actors[0].Name);
        Assert.Equal(3, actors[0].FilmCount);
        Assert.Equal(3, actors.Single(a => a.Name == "Tony Leung Chiu-wai").FilmCount);
    }

    [Fact]
    public void Countries_count_coproductions_for_each_country()
    {
        var countries = _catalog.GetCountries();

        Assert.Equal("HK", countries[0].Country.Code);
        Assert.Equal(5, countries[0].FilmCount);
        Assert.Equal(1, countries.Single(c => c.Country.Code == "FR").FilmCount);
    }

    [Fact]
    public void Countries_list_decades_and_main_directors()
    {
        CountrySummary hongKong = _catalog.GetCountries()[0];

        Assert.Equal([1980, 1990, 2000], [.. hongKong.Decades.Select(d => d.Decade)]);
        Assert.Equal([2, 2, 1], [.. hongKong.Decades.Select(d => d.FilmCount)]);
        Assert.Equal(["John Woo", "Wong Kar-wai"], hongKong.Directors.Select(d => d.Name));
    }

    [Fact]
    public void Filter_combines_country_and_director_oldest_first()
    {
        var films = _catalog.Filter(new FilmFilter(Country: "HK", Person: "john woo", Role: PersonRole.Director)).ToArray();

        Assert.Equal(["A Better Tomorrow", "The Killer", "Hard Boiled"], [.. films.Select(f => f.Name)]);
    }

    [Fact]
    public void Filter_by_decade()
    {
        var films = _catalog.Filter(new FilmFilter(Country: "JP", Decade: 1940)).ToArray();

        Assert.Equal(["Late Spring"], [.. films.Select(f => f.Name)]);
    }

    [Fact]
    public void Filter_by_actor()
    {
        var films = _catalog.Filter(new FilmFilter(Person: "Tony Leung Chiu-wai", Role: PersonRole.Actor)).ToArray();

        Assert.Equal(3, films.Length);
    }

    [Fact]
    public void Filter_by_movement()
    {
        var films = _catalog.Filter(new FilmFilter(Movement: "heroic-bloodshed")).ToArray();

        Assert.Equal(["A Better Tomorrow", "The Killer", "Hard Boiled"], [.. films.Select(f => f.Name)]);
    }

    [Fact]
    public void Filter_by_unknown_movement_returns_nothing()
        => Assert.Empty(_catalog.Filter(new FilmFilter(Movement: "nope")));

    [Fact]
    public void Movements_report_their_film_counts()
    {
        var movements = _catalog.GetMovements();

        Assert.Equal(3, movements.Single(m => m.Movement.Id == "heroic-bloodshed").FilmCount);
        Assert.Equal(1, movements.Single(m => m.Movement.Id == "hong-kong-new-wave").FilmCount);
    }

    [Fact]
    public void CountByDecade_skips_films_without_a_year()
    {
        var decades = FilmCatalog.CountByDecade([Film("A", 1961), Film("B", 1969), Film("C"), Film("D", 1970)]);

        Assert.Equal([new DecadeCount(1960, 2), new DecadeCount(1970, 1)], decades);
    }

    [Fact]
    public void People_with_a_tmdb_id_are_merged_across_spellings_and_scripts()
    {
        FilmCatalog catalog = new(
            [
                Film("The Mission", 1999, ["Hong Kong"], ["杜琪峯@25236"]),
                Film("Election", 2005, ["Hong Kong"], ["Johnnie To@25236"]),
                Film("Exiled", 2006, ["Hong Kong"], ["Johnnie To@25236"]),
            ],
            []);

        PersonSummary to = Assert.Single(catalog.GetPeople(PersonRole.Director));
        Assert.Equal("tmdb:25236", to.Key);
        Assert.Equal("Johnnie To", to.Name);
        Assert.Equal(3, to.FilmCount);
    }

    [Fact]
    public void Homonyms_with_different_tmdb_ids_stay_apart()
    {
        FilmCatalog catalog = new([Film("A", 2000, directors: ["John Smith@1"]), Film("B", 2001, directors: ["John Smith@2"])], []);

        Assert.Equal(2, catalog.GetPeople(PersonRole.Director).Count);
    }

    [Fact]
    public void Credits_without_a_tmdb_id_borrow_the_one_their_name_leads_to()
    {
        FilmCatalog catalog = new([Film("A", 1960, directors: ["Jean-Luc Godard@3776"]), Film("B", 1961, directors: ["Jean-Luc Godard"])], []);

        PersonSummary godard = Assert.Single(catalog.GetPeople(PersonRole.Director));
        Assert.Equal(2, godard.FilmCount);
        Assert.Equal("3776", godard.TmdbId);
    }

    [Fact]
    public void Ambiguous_names_do_not_lend_a_tmdb_id()
    {
        FilmCatalog catalog = new(
            [Film("A", directors: ["John Smith@1"]), Film("B", directors: ["John Smith@2"]), Film("C", directors: ["John Smith"])],
            []);

        Assert.Equal(3, catalog.GetPeople(PersonRole.Director).Count);
    }

    [Fact]
    public void Filter_by_tmdb_key_finds_every_spelling()
    {
        FilmCatalog catalog = new([Film("The Mission", 1999, directors: ["杜琪峯@25236"]), Film("Election", 2005, directors: ["Johnnie To@25236"])], []);

        Assert.Equal(2, catalog.Filter(new FilmFilter(Person: "tmdb:25236", Role: PersonRole.Director)).Count());
    }

    [Fact]
    public void Filter_by_person_without_role_searches_every_role()
    {
        FilmCatalog catalog = new([Film("A", directors: ["Takeshi Kitano"]), Film("B", actors: ["Takeshi Kitano"])], []);

        Assert.Equal(2, catalog.Filter(new FilmFilter(Person: "Takeshi Kitano")).Count());
    }

    [Theory]
    [InlineData(new[] { "杜琪峯", "Johnnie To" }, "Johnnie To")]
    [InlineData(new[] { "杜琪峯", "杜琪峯", "Johnnie To" }, "杜琪峯")]
    public void ChooseName_prefers_the_most_frequent_then_latin_spelling(string[] names, string expected)
        => Assert.Equal(expected, FilmCatalog.ChooseName(names));

    [Fact]
    public void Writers_are_listed_like_directors()
    {
        FilmCatalog catalog = new(
            [
                Film("The Good, the Bad and the Ugly", 1966, ["Italy"], writers: ["Sergio Leone@100", "Luciano Vincenzoni"]),
                Film("Once Upon a Time in the West", 1968, ["Italy"], writers: ["Sergio Leone@100"]),
            ],
            []);

        PersonSummary leone = catalog.GetPeople(PersonRole.Writer)[0];
        Assert.Equal("Sergio Leone", leone.Name);
        Assert.Equal(2, leone.FilmCount);
        Assert.Equal(2, catalog.Filter(new FilmFilter(Person: "tmdb:100", Role: PersonRole.Writer)).Count());
    }
}
