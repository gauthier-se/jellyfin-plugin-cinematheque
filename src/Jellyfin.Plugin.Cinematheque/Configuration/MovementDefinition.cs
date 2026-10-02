using System;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// A curated film movement, such as the French New Wave, described as a set of rules.
/// </summary>
/// <remarks>
/// A film belongs to the movement when its TMDB id is listed in <see cref="TmdbIds"/>, or when it
/// satisfies every rule that is set: one of the countries, inside the year range, by one of the
/// directors, with one of the genres, with one of the tags. Empty rules are ignored, and a movement
/// with no country, director, genre or tag rule only matches its explicit TMDB ids.
/// </remarks>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays keep the XML configuration round trip replace-only.")]
public class MovementDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier, used in URLs.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a short description shown on the movement card.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country codes, any of which qualifies.
    /// </summary>
    public string[] Countries { get; set; } = [];

    /// <summary>
    /// Gets or sets the first year of the movement, inclusive.
    /// </summary>
    public int? YearFrom { get; set; }

    /// <summary>
    /// Gets or sets the last year of the movement, inclusive.
    /// </summary>
    public int? YearTo { get; set; }

    /// <summary>
    /// Gets or sets the directors, any of whom qualifies: a name, <c>tmdb:25236</c>, or both as
    /// <c>Johnnie To (tmdb:25236)</c>. See <see cref="Catalog.PersonRef"/>.
    /// </summary>
    public string[] Directors { get; set; } = [];

    /// <summary>
    /// Gets or sets the genres, any of which qualifies.
    /// </summary>
    public string[] Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags, any of which qualifies.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets TMDB ids of films that belong to the movement regardless of the other rules.
    /// </summary>
    public string[] TmdbIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the name and description in other languages. <see cref="Name"/> and
    /// <see cref="Description"/> are the fallback.
    /// </summary>
    public MovementTranslation[] Translations { get; set; } = [];

    /// <summary>
    /// Gets the name and description in a language, falling back to the base language and then
    /// to the untranslated text.
    /// </summary>
    /// <param name="language">A language code such as <c>fr</c> or <c>fr-CA</c>.</param>
    /// <returns>The name and description.</returns>
    public (string Name, string Description) Localize(string? language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            string baseLanguage = language.Split('-', '_')[0];
            MovementTranslation? translation =
                Array.Find(Translations ?? [], t => string.Equals(t.Language, language, StringComparison.OrdinalIgnoreCase))
                ?? Array.Find(Translations ?? [], t => string.Equals(t.Language, baseLanguage, StringComparison.OrdinalIgnoreCase));
            if (translation is not null && !string.IsNullOrWhiteSpace(translation.Name))
            {
                return (translation.Name, string.IsNullOrWhiteSpace(translation.Description) ? Description : translation.Description);
            }
        }

        return (Name, Description);
    }

    /// <summary>
    /// Adds a translation. Convenience for the built-in defaults.
    /// </summary>
    /// <param name="language">The language code.</param>
    /// <param name="name">The translated name.</param>
    /// <param name="description">The translated description.</param>
    /// <returns>This movement.</returns>
    public MovementDefinition WithTranslation(string language, string name, string description)
    {
        Translations = [.. Translations ?? [], new MovementTranslation { Language = language, Name = name, Description = description }];
        return this;
    }

    /// <summary>
    /// Creates a movement from its rules. Convenience for the built-in defaults and tests.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="countries">The country codes.</param>
    /// <param name="years">The inclusive year range.</param>
    /// <param name="directors">The directors.</param>
    /// <param name="genres">The genres.</param>
    /// <returns>The movement.</returns>
    public static MovementDefinition Create(
        string id,
        string name,
        string description,
        string[] countries,
        (int From, int To) years,
        string[] directors,
        string[]? genres = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return new MovementDefinition
        {
            Id = id,
            Name = name,
            Description = description,
            Countries = countries,
            YearFrom = years.From,
            YearTo = years.To,
            Directors = directors,
            Genres = genres ?? [],
        };
    }
}
