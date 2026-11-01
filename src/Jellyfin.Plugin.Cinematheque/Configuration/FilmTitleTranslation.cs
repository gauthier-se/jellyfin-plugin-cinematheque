namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The title of an essential film in one language.
/// </summary>
public class FilmTitleTranslation
{
    /// <summary>
    /// Gets or sets the language, as a code such as <c>fr</c> or <c>pt-BR</c>.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the translated title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
}
