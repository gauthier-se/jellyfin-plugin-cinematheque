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
    public void A_movement_without_rules_only_matches_its_tmdb_ids()
    {
        MovementMatcher matcher = new(new MovementDefinition { Id = "empty", YearFrom = 1950, YearTo = 1960 });

        Assert.False(matcher.Matches(Film("Film", 1955, ["France"])));
    }
}
