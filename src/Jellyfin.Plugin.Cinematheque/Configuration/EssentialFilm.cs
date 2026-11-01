using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// A film that defines a movement. Its title is stored because the film may not be in the library.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays keep the XML configuration round trip replace-only.")]
public class EssentialFilm
{
    /// <summary>
    /// Gets or sets the TMDB id.
    /// </summary>
    public string TmdbId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the English title, the fallback for every language.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the title in other languages.
    /// </summary>
    public FilmTitleTranslation[] Translations { get; set; } = [];

    /// <summary>
    /// Gets the title in a language, falling back to the base language and then to the English title.
    /// </summary>
    /// <param name="language">A language code such as <c>fr</c> or <c>fr-CA</c>.</param>
    /// <returns>The title.</returns>
    public string Localize(string? language)
    {
        FilmTitleTranslation? translation = Translations.FindForLanguage(language, t => t.Language);
        return string.IsNullOrWhiteSpace(translation?.Title) ? Title : translation.Title;
    }
}
