namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The name and description of a movement in one language.
/// </summary>
public class MovementTranslation
{
    /// <summary>
    /// Gets or sets the language, as a code such as <c>fr</c> or <c>pt-BR</c>.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the translated name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the translated description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
