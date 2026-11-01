using System.Text.Json;
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
            movement.Countries.Length + movement.Directors.Length + movement.Genres.Length + movement.Essentials.Length > 0,
            $"{id} has no rule and no film.");
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Essentials_are_complete_and_chronological(string id)
    {
        EssentialFilm[] essentials = Find(id).Essentials;

        Assert.True(essentials.Length >= 10, $"{id} lists {essentials.Length} essential films.");
        Assert.All(essentials, film =>
        {
            Assert.Matches(Number(), film.TmdbId);
            Assert.False(string.IsNullOrWhiteSpace(film.Title));
            Assert.NotNull(film.Year);
        });
        Assert.Equal(essentials.OrderBy(f => f.Year), essentials);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Film_lists_do_not_contradict_each_other(string id)
    {
        MovementDefinition movement = Find(id);
        string[] included = [.. movement.Essentials.Select(f => f.TmdbId), .. movement.TmdbIds];

        Assert.All(movement.TmdbIds.Concat(movement.ExcludedTmdbIds), tmdbId => Assert.Matches(Number(), tmdbId));
        Assert.Equal(included.Length, included.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(included.Intersect(movement.ExcludedTmdbIds, StringComparer.Ordinal));
    }

    [Fact]
    public void Every_film_list_belongs_to_a_movement()
    {
        using Stream stream = typeof(DefaultMovements).Assembly.GetManifestResourceStream("Jellyfin.Plugin.Cinematheque.Configuration.MovementFilms.json")!;
        using JsonDocument document = JsonDocument.Parse(stream);

        Assert.Equal(
            _movements.Select(m => m.Id).Order(StringComparer.Ordinal),
            document.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    private static MovementDefinition Find(string id) => _movements.Single(m => m.Id == id);

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex Number();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex UrlSafe();

    [GeneratedRegex(@"^\S.* \(tmdb:[0-9]+\)$")]
    private static partial Regex NameAndTmdbId();
}
