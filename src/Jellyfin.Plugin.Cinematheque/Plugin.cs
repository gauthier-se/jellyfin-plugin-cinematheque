using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Cinematheque.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Cinematheque;

/// <summary>
/// The Cinematheque plugin: browse a film library by director, actor, country and movement.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// The plugin id. Never change it: Jellyfin keys the configuration and the install on it.
    /// </summary>
    public static readonly Guid PluginId = Guid.Parse("6f86a1dd-c4dc-4aa6-a445-8f19be1087c1");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        if (Configuration.MigrateLegacyMovements())
        {
            SaveConfiguration();
        }
    }

    /// <inheritdoc />
    public override string Name => "Cinematheque";

    /// <inheritdoc />
    public override string Description => "Browse your films by director, actor, country and film movement.";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace),
            },
        ];
    }
}
