using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The plugin configuration, edited from the plugin's dashboard page.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays keep the XML configuration round trip replace-only.")]
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets how many actors per film count, in billing order.
    /// </summary>
    /// <remarks>
    /// Full casts run to dozens of names. Keeping the top of the bill turns the actor list into
    /// one of leading players instead of a census of extras.
    /// </remarks>
    public int ActorsPerFilm { get; set; } = 10;

    /// <summary>
    /// Gets or sets the minimum number of films for an actor to be listed by default.
    /// </summary>
    public int MinActorFilms { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum number of films for a director or screenwriter to be
    /// listed by default.
    /// </summary>
    public int MinDirectorFilms { get; set; } = 1;

    /// <summary>
    /// Gets or sets the curated movements.
    /// </summary>
    public MovementDefinition[] Movements { get; set; } = DefaultMovements.Create();
}
