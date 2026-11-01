using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using static Jellyfin.Plugin.Cinematheque.Tests.FilmFactory;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class MovementMatcherTests
{
    private static readonly MovementMatcher _newWave = new(MovementDefinition.Create(
        "french-new-wave",
        "French New Wave",
        string.Empty,
        ["FR"],
        (1958, 1973),
        ["Jean-Luc Godard", "François Truffaut"]));

    [Fact]
    public void Matches_a_film_that_satisfies_every_rule()
        => Assert.True(_newWave.Matches(Film("Breathless", 1960, ["France"], ["Jean-Luc Godard"])));

    [Fact]
    public void Matches_director_names_without_diacritics()
        => Assert.True(_newWave.Matches(Film("The 400 Blows", 1959, ["France"], ["Francois Truffaut"])));

    [Theory]
    [InlineData(1957)]
    [InlineData(1974)]
    public void Rejects_films_outside_the_period(int year)
        => Assert.False(_newWave.Matches(Film("Film", year, ["France"], ["Jean-Luc Godard"])));

    [Fact]
    public void Rejects_films_without_a_year_when_the_movement_has_a_period()
        => Assert.False(_newWave.Matches(Film("Film", null, ["France"], ["Jean-Luc Godard"])));

    [Fact]
    public void Rejects_films_from_another_country()
        => Assert.False(_newWave.Matches(Film("Film", 1965, ["Switzerland"], ["Jean-Luc Godard"])));

    [Fact]
    public void Accepts_coproductions_that_include_the_country()
        => Assert.True(_newWave.Matches(Film("Contempt", 1963, ["Italy", "France"], ["Jean-Luc Godard"])));

    [Fact]
    public void Rejects_films_by_other_directors()
        => Assert.False(_newWave.Matches(Film("Film", 1965, ["France"], ["Henri Verneuil"])));

    [Fact]
    public void Accepts_country_names_in_the_configuration()
    {
        MovementMatcher matcher = new(MovementDefinition.Create("x", "X", string.Empty, ["Hong Kong"], (1980, 1990), []));

        Assert.True(matcher.Matches(Film("Film", 1985, ["Hong Kong"])));
    }

    [Fact]
    public void Matches_genres_when_no_director_is_listed()
    {
        MovementMatcher spaghetti = new(MovementDefinition.Create("sw", "Spaghetti Western", string.Empty, ["IT"], (1960, 1978), [], ["Western"]));

        Assert.True(spaghetti.Matches(Film("Once Upon a Time in the West", 1968, ["Italy", "United States of America"], genres: ["Western"])));
        Assert.False(spaghetti.Matches(Film("The Leopard", 1963, ["Italy"], genres: ["Drama"])));
    }

    [Fact]
    public void Explicit_tmdb_ids_bypass_the_rules()
    {
        MovementDefinition movement = MovementDefinition.Create("x", "X", string.Empty, ["FR"], (1958, 1973), ["Jean-Luc Godard"]);
        movement.TmdbIds = ["8423"];
        MovementMatcher matcher = new(movement);

        Assert.True(matcher.Matches(Film("Film", 1990, ["Japan"], tmdbId: "8423")));
    }

    [Fact]
    public void Essential_films_bypass_the_rules()
    {
        MovementDefinition movement = MovementDefinition.Create("x", "X", string.Empty, ["DE"], (1919, 1933), ["Fritz Lang"]);
        movement.Essentials = [new EssentialFilm { TmdbId = "631", Title = "Sunrise", Year = 1927 }];
        MovementMatcher matcher = new(movement);

        Assert.True(matcher.Matches(Film("Sunrise", 1927, ["United States of America"], ["F. W. Murnau"], tmdbId: "631")));
    }

    [Fact]
    public void Excluded_tmdb_ids_beat_the_rules()
    {
        MovementDefinition movement = MovementDefinition.Create("x", "X", string.Empty, ["DE"], (1919, 1933), ["Fritz Lang"]);
        movement.ExcludedTmdbIds = ["5206"];
        MovementMatcher matcher = new(movement);

        Assert.False(matcher.Matches(Film("Woman in the Moon", 1929, ["Germany"], ["Fritz Lang"], tmdbId: "5206")));
        Assert.True(matcher.Matches(Film("Metropolis", 1927, ["Germany"], ["Fritz Lang"], tmdbId: "19")));
    }

    [Fact]
    public void Excluded_tmdb_ids_beat_explicit_ids_and_essentials()
    {
        MovementDefinition movement = new()
        {
            Id = "x",
            TmdbIds = ["1"],
            Essentials = [new EssentialFilm { TmdbId = "2", Title = "Two" }, new EssentialFilm { TmdbId = "3", Title = "Three" }],
            ExcludedTmdbIds = ["1", " 2 "],
        };
        MovementMatcher matcher = new(movement);

        Assert.False(matcher.Matches(Film("One", tmdbId: "1")));
        Assert.False(matcher.Matches(Film("Two", tmdbId: "2")));
        Assert.Equal(["Three"], matcher.Essentials.Select(f => f.Title));
    }

    [Fact]
    public void Essentials_without_a_tmdb_id_are_ignored()
    {
        MovementDefinition movement = new() { Id = "x", Essentials = [new EssentialFilm { Title = "No id" }] };

        Assert.Empty(new MovementMatcher(movement).Essentials);
    }

    [Fact]
    public void A_saved_movement_may_have_no_film_lists()
    {
        MovementDefinition movement = new() { Id = "x", Essentials = null!, ExcludedTmdbIds = null!, TmdbIds = null! };

        Assert.False(new MovementMatcher(movement).Matches(Film("Film", tmdbId: "1")));
    }

    [Fact]
    public void A_movement_without_rules_only_matches_its_tmdb_ids()
    {
        MovementMatcher matcher = new(new MovementDefinition { Id = "empty", YearFrom = 1950, YearTo = 1960 });

        Assert.False(matcher.Matches(Film("Film", 1955, ["France"])));
    }

    [Fact]
    public void Director_tmdb_references_match_credits_in_any_script()
    {
        MovementMatcher matcher = new(MovementDefinition.Create("hb", "Heroic Bloodshed", string.Empty, ["HK"], (1986, 1998), ["Johnnie To (tmdb:25236)"]));

        Assert.True(matcher.Matches(Film("Lifeline", 1997, ["Hong Kong"], ["杜琪峯@25236"])));
        Assert.True(matcher.Matches(Film("A Hero Never Dies", 1998, ["Hong Kong"], ["Johnnie To"])));
    }
}
