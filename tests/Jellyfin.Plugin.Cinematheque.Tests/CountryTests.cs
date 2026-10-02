using Jellyfin.Plugin.Cinematheque.Catalog;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class CountryTests
{
    [Theory]
    [InlineData("United States of America", "US")]
    [InlineData("USA", "US")]
    [InlineData("Hong Kong", "HK")]
    [InlineData("hong kong", "HK")]
    [InlineData("Japan", "JP")]
    [InlineData("Italy", "IT")]
    [InlineData("South Korea", "KR")]
    [InlineData("Korea, Republic of", "KR")]
    [InlineData("Côte d'Ivoire", "CI")]
    [InlineData("West Germany", "DE")]
    public void FromLocation_resolves_known_names_to_iso_codes(string location, string code)
        => Assert.Equal(code, Country.FromLocation(location)!.Code);

    [Theory]
    [InlineData("Soviet Union", "SU")]
    [InlineData("Czechoslovakia", "XC")]
    [InlineData("East Germany", "XG")]
    [InlineData("Yugoslavia", "YU")]
    public void FromLocation_keeps_historical_states_apart(string location, string code)
        => Assert.Equal(code, Country.FromLocation(location)!.Code);

    [Fact]
    public void FromLocation_uses_the_canonical_name()
        => Assert.Equal("United States of America", Country.FromLocation("USA")!.Name);

    [Fact]
    public void FromLocation_slugs_unknown_names()
    {
        Country country = Country.FromLocation("  Republic of Somewhere  ")!;

        Assert.Equal("republic-of-somewhere", country.Code);
        Assert.Equal("Republic of Somewhere", country.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromLocation_ignores_blanks(string? location)
        => Assert.Null(Country.FromLocation(location));

    [Fact]
    public void FromLocations_drops_duplicates_and_keeps_order()
    {
        var countries = Country.FromLocations(["Japan", "France", "Japan", "", "Hong Kong"]);

        Assert.Equal(["JP", "FR", "HK"], [.. countries.Select(c => c.Code)]);
    }
}
