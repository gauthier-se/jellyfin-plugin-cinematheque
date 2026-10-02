using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.Cinematheque.Integration;

/// <summary>
/// Registers the index.html patch with the File Transformation plugin at server startup.
/// </summary>
/// <remarks>
/// File Transformation lives in its own assembly load context, so it cannot be referenced at
/// compile time; its documented entry point is called through reflection instead. A startup
/// task rather than a hosted service, because by then every plugin assembly is loaded.
/// </remarks>
public class WebInjectionTask : IScheduledTask, IConfigurableScheduledTask
{
    private static readonly Guid _transformationId = Guid.Parse("5b0a7d0e-3b52-4c63-9f1e-2a4f6c9e8d11");

    private readonly ILogger<WebInjectionTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebInjectionTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WebInjectionTask(ILogger<WebInjectionTask> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Cinematheque web integration";

    /// <inheritdoc />
    public string Key => "CinemathequeWebInjection";

    /// <inheritdoc />
    public string Description => "Adds the Cinematheque tab to the web client through the File Transformation plugin.";

    /// <inheritdoc />
    public string Category => "Cinematheque";

    /// <inheritdoc />
    public bool IsHidden => true;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => false;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        Assembly? fileTransformation = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(assembly => assembly.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);

        MethodInfo? register = fileTransformation?
            .GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?
            .GetMethod("RegisterTransformation");

        if (register is null)
        {
            _logger.LogWarning(
                "The File Transformation plugin is not installed, so the Cinematheque tab cannot be added to the web client. "
                + "The Cinematheque API still works. Install File Transformation and restart the server to get the tab.");
            return Task.CompletedTask;
        }

        JObject payload = new JObject
        {
            { "id", _transformationId.ToString() },
            { "fileNamePattern", "index.html" },
            { "callbackAssembly", typeof(IndexHtmlPatch).Assembly.FullName },
            { "callbackClass", typeof(IndexHtmlPatch).FullName },
            { "callbackMethod", nameof(IndexHtmlPatch.Apply) },
        };

        try
        {
            register.Invoke(null, [payload]);
            _logger.LogInformation("Registered the Cinematheque web client integration with File Transformation");
        }
        catch (TargetInvocationException ex)
        {
            _logger.LogError(ex.InnerException ?? ex, "File Transformation rejected the Cinematheque integration");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
    }
}
