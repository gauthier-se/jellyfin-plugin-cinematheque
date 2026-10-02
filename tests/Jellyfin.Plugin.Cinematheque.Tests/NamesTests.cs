using Jellyfin.Plugin.Cinematheque.Catalog;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class NamesTests
{
    [Theory]
    [InlineData("Nagisa Ōshima", "Nagisa Oshima")]
    [InlineData("F.W. Murnau", "F. W. Murnau")]
    [InlineData("WONG KAR-WAI", "Wong Kar-wai")]
    [InlineData("Éric Rohmer", "Eric Rohmer")]
    public void Key_treats_spelling_variants_as_the_same_name(string left, string right)
        => Assert.Equal(Names.Key(left), Names.Key(right));

    [Fact]
    public void Key_keeps_different_names_apart()
        => Assert.NotEqual(Names.Key("Kiju Yoshida"), Names.Key("Yoshishige Yoshida"));

    [Theory]
    [InlineData("Republic of Somewhere", "republic-of-somewhere")]
    [InlineData("São Tomé & Príncipe", "sao-tome-principe")]
    [InlineData("  Trailing  ", "trailing")]
    public void Slug_is_url_friendly(string value, string slug)
        => Assert.Equal(slug, Names.Slug(value));
}
