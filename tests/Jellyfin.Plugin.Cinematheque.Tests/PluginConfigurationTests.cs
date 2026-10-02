using Jellyfin.Plugin.Cinematheque.Configuration;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class PluginConfigurationTests
{
    private static readonly string[] _builtInIds = [.. DefaultMovements.Create().Select(m => m.Id)];

    [Fact]
    public void A_fresh_configuration_uses_every_built_in()
        => Assert.Equal(_builtInIds, new PluginConfiguration().GetEffectiveMovements().Select(m => m.Id));

    [Fact]
    public void Hidden_built_ins_are_left_out()
    {
        PluginConfiguration configuration = new() { HiddenMovements = ["spaghetti-western"] };

        Assert.DoesNotContain(configuration.GetEffectiveMovements(), m => m.Id == "spaghetti-western");
        Assert.Equal(_builtInIds.Length - 1, configuration.GetEffectiveMovements().Count);
    }

    [Fact]
    public void An_edited_built_in_replaces_the_original_in_place()
    {
        PluginConfiguration configuration = new() { CustomMovements = [new MovementDefinition { Id = "french-new-wave", Name = "La Nouvelle Vague" }] };

        IReadOnlyList<MovementDefinition> movements = configuration.GetEffectiveMovements();
        Assert.Equal(_builtInIds, movements.Select(m => m.Id));
        Assert.Equal("La Nouvelle Vague", movements.Single(m => m.Id == "french-new-wave").Name);
    }

    [Fact]
    public void Added_movements_come_after_the_built_ins()
    {
        PluginConfiguration configuration = new() { CustomMovements = [new MovementDefinition { Id = "shaw-brothers", Name = "Shaw Brothers" }] };

        Assert.Equal("shaw-brothers", configuration.GetEffectiveMovements()[^1].Id);
    }

    [Fact]
    public void Migration_drops_saved_copies_of_built_ins_and_keeps_additions()
    {
        PluginConfiguration configuration = new()
        {
            Movements = [new MovementDefinition { Id = "french-new-wave", Name = "Old copy" }, new MovementDefinition { Id = "giallo", Name = "Giallo" }],
        };

        Assert.True(configuration.MigrateLegacyMovements());
        Assert.Empty(configuration.Movements);
        Assert.Equal(["giallo"], configuration.CustomMovements.Select(m => m.Id));
        Assert.Equal("French New Wave", configuration.GetEffectiveMovements().Single(m => m.Id == "french-new-wave").Name);
    }

    [Fact]
    public void Migration_does_nothing_on_a_current_configuration()
        => Assert.False(new PluginConfiguration().MigrateLegacyMovements());

    [Theory]
    [InlineData("fr", "Nouvelle Vague")]
    [InlineData("fr-CA", "Nouvelle Vague")]
    [InlineData("de", "French New Wave")]
    [InlineData(null, "French New Wave")]
    public void Movements_are_localized_with_a_fallback(string? language, string expected)
        => Assert.Equal(expected, DefaultMovements.Create().Single(m => m.Id == "french-new-wave").Localize(language).Name);

    [Fact]
    public void Every_built_in_has_a_french_translation()
        => Assert.All(DefaultMovements.Create(), m => Assert.Contains(m.Translations, t => t.Language == "fr"));
}
