namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// The query string filters shared by the film endpoints.
/// </summary>
public class FilmQuery
{
    /// <summary>
    /// Gets or sets a country code.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets a person key, such as <c>tmdb:25236</c>, or a name.
    /// </summary>
    public string? Person { get; set; }

    /// <summary>
    /// Gets or sets the role the person is credited in, such as <c>directors</c>. Any role when unset.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets a director name. Deprecated: use <see cref="Person"/> and <see cref="Role"/>.
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// Gets or sets an actor name. Deprecated: use <see cref="Person"/> and <see cref="Role"/>.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets a movement id.
    /// </summary>
    public string? Movement { get; set; }

    /// <summary>
    /// Gets or sets a decade, such as 1960.
    /// </summary>
    public int? Decade { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Country"/> must be the first listed
    /// production country.
    /// </summary>
    public bool PrimaryCountry { get; set; }
}
