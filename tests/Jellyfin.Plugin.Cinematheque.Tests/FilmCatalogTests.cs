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
        Assert.Equal(["John Woo", "Wong Kar-wai"], hongKong.Directors);
    }

    [Fact]
    public void Filter_combines_country_and_director_oldest_first()
    {
        var films = _catalog.Filter(new FilmFilter(Country: "HK", Director: "john woo")).ToArray();

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
        var films = _catalog.Filter(new FilmFilter(Actor: "Tony Leung Chiu-wai")).ToArray();

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
}
