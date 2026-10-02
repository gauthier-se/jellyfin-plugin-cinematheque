using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The plugin configuration, edited from the plugin's dashboard page.
/// </summary>
/// <remarks>
/// Built-in movements live in <see cref="DefaultMovements"/>, not here, so they improve with each
/// release. The configuration only records what the administrator changed: built-ins they edited
/// or added (<see cref="CustomMovements"/>) and built-ins they hid (<see cref="HiddenMovements"/>).
/// </remarks>
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
    /// Gets or sets movements the administrator added, and built-ins they edited (same id).
    /// </summary>
    public MovementDefinition[] CustomMovements { get; set; } = [];

    /// <summary>
    /// Gets or sets the ids of built-in movements the administrator hid.
    /// </summary>
    public string[] HiddenMovements { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether each movement gets a Jellyfin collection, so the
    /// movements also show up in apps that do not load the Cinematheque tab.
    /// </summary>
    public bool SyncCollections { get; set; }

    /// <summary>
    /// Gets or sets the collections the plugin created. It never deletes them: a collection whose
    /// movement is hidden or removed, or whose sync is turned off, is simply left alone.
    /// </summary>
    public MovementCollectionLink[] ManagedCollections { get; set; } = [];

    /// <summary>
    /// Gets or sets the full movement list saved by version 0.1. Only read to migrate it.
    /// </summary>
    public MovementDefinition[] Movements { get; set; } = [];

    /// <summary>
    /// Lists the movements in effect: built-ins in their order, replaced by the administrator's
    /// version when there is one and minus the hidden ones, then the administrator's additions.
    /// </summary>
    /// <returns>The movements.</returns>
    public IReadOnlyList<MovementDefinition> GetEffectiveMovements()
    {
        Dictionary<string, MovementDefinition> custom = (CustomMovements ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
        HashSet<string> hidden = new HashSet<string>(HiddenMovements ?? [], StringComparer.OrdinalIgnoreCase);
        MovementDefinition[] builtIn = DefaultMovements.Create();
        HashSet<string> builtInIds = new HashSet<string>(builtIn.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);

        return builtIn
            .Where(m => !hidden.Contains(m.Id))
            .Select(m => custom.GetValueOrDefault(m.Id) ?? m)
            .Concat(custom.Values.Where(c => !builtInIds.Contains(c.Id)))
            .ToArray();
    }

    /// <summary>
    /// Moves a version 0.1 movement list to the current model.
    /// </summary>
    /// <returns><c>true</c> when the configuration changed and should be saved.</returns>
    /// <remarks>
    /// Version 0.1 saved a copy of every built-in. Copies of built-ins are dropped so the current
    /// built-ins apply; movements with other ids were added by the administrator and are kept.
    /// Edits to built-ins made in 0.1 are not carried over: 0.1 was public for a single day.
    /// </remarks>
    public bool MigrateLegacyMovements()
    {
        if (Movements is null || Movements.Length == 0)
        {
            return false;
        }

        HashSet<string> builtInIds = new HashSet<string>(DefaultMovements.Create().Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
        CustomMovements = [.. CustomMovements ?? [], .. Movements.Where(m => !builtInIds.Contains(m.Id))];
        Movements = [];
        return true;
    }
}
