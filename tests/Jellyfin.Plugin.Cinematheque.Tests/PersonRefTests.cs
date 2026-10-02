using Jellyfin.Plugin.Cinematheque.Catalog;
using static Jellyfin.Plugin.Cinematheque.Tests.FilmFactory;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class PersonRefTests
{
    [Fact]
    public void Tmdb_reference_matches_the_id_only()
    {
        PersonRef reference = PersonRef.Parse("tmdb:25236")!;

        Assert.True(reference.Matches(Credit("杜琪峯@25236")));
        Assert.False(reference.Matches(Credit("Johnnie To")));
    }

    [Fact]
    public void Name_with_tmdb_reference_matches_either()
    {
        PersonRef reference = PersonRef.Parse("Johnnie To (tmdb:25236)")!;

        Assert.True(reference.Matches(Credit("杜琪峯@25236")));
        Assert.True(reference.Matches(Credit("Johnnie To")));
        Assert.False(reference.Matches(Credit("John Woo@11401")));
    }

    [Theory]
    [InlineData("Nagisa Ōshima")]
    [InlineData("name:nagisaoshima")]
    [InlineData("NAGISA OSHIMA")]
    public void Name_references_ignore_accents_and_case(string value)
        => Assert.True(PersonRef.Parse(value)!.Matches(Credit("Nagisa Oshima@1")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Blank_references_name_nobody(string? value)
        => Assert.Null(PersonRef.Parse(value));

    [Fact]
    public void Overlong_references_are_rejected()
        => Assert.Null(PersonRef.Parse(new string('a', 300) + " (tmdb:1)"));

    [Fact]
    public void Credit_key_prefers_the_tmdb_id()
    {
        Assert.Equal("tmdb:25236", Credit("杜琪峯@25236").Key);
        Assert.Equal("name:johnnieto", Credit("Johnnie To").Key);
    }
}
