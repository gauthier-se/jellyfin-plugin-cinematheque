using System.Text.RegularExpressions;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Tests;

/// <summary>
/// Mistakes in the built-in data fail silently at run time: an unknown country code or a mistyped
/// reference only makes a movement lose its films. These tests catch them before a release.
/// </summary>
public partial class DefaultMovementsTests
{
    private static readonly MovementDefinition[] _movements = DefaultMovements.Create();

    public static TheoryData<string> Ids => [.. _movements.Select(m => m.Id)];

    [Fact]
    public void Ids_are_unique()
        => Assert.Equal(_movements.Length, _movements.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

    [Theory]
    [MemberData(nameof(Ids))]
    public void Ids_are_safe_in_a_url(string id)
        => Assert.Matches(UrlSafe(), id);

    [Theory]
    [MemberData(nameof(Ids))]
    public void Movements_have_a_name_a_description_and_a_period(string id)
    {
        MovementDefinition movement = Find(id);

        Assert.False(string.IsNullOrWhiteSpace(movement.Name));
        Assert.False(string.IsNullOrWhiteSpace(movement.Description));
        Assert.True(movement.YearFrom <= movement.YearTo, $"{id} ends before it starts.");
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Countries_are_codes_the_catalog_knows(string id)
        // An unknown value still resolves, to a slug of itself that no film will ever have.
        => Assert.All(Find(id).Countries, code => Assert.Equal(code, Country.FromLocation(code)?.Code));

    [Theory]
    [MemberData(nameof(Ids))]
    public void Directors_carry_their_name_and_tmdb_id(string id)
        => Assert.All(Find(id).Directors, director => Assert.Matches(NameAndTmdbId(), director));

    [Theory]
    [MemberData(nameof(Ids))]
    public void Directors_are_listed_once(string id)
    {
        string[] directors = Find(id).Directors;

        Assert.Equal(directors.Length, directors.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Movements_match_something(string id)
    {
        MovementDefinition movement = Find(id);

        Assert.True(
            movement.Countries.Length + movement.Directors.Length + movement.Genres.Length + movement.TmdbIds.Length > 0,
            $"{id} has no rule and no film.");
    }

    private static MovementDefinition Find(string id) => _movements.Single(m => m.Id == id);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex UrlSafe();

    [GeneratedRegex(@"^\S.* \(tmdb:[0-9]+\)$")]
    private static partial Regex NameAndTmdbId();
}
