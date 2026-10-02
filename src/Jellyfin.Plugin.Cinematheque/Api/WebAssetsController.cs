using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// Serves the web client scripts and styles embedded in the plugin.
/// </summary>
/// <remarks>
/// Anonymous on purpose: the loader is requested by index.html before anyone signs in. The files
/// hold no data; everything they display comes from the authenticated API.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("Cinematheque/Web")]
public class WebAssetsController : ControllerBase
{
    private static readonly FrozenDictionary<string, string> _assets = new Dictionary<string, string>
    {
        ["loader.js"] = "text/javascript",
        ["app.js"] = "text/javascript",
        ["app.css"] = "text/css",
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets an embedded web asset.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <returns>The file.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetAsset([FromRoute] string name)
    {
        if (!_assets.TryGetValue(name, out string? contentType))
        {
            return NotFound();
        }

        Stream? stream = typeof(WebAssetsController).Assembly
            .GetManifestResourceStream($"{typeof(Plugin).Namespace}.Web.{name}");
        if (stream is null)
        {
            return NotFound();
        }

        // URLs carry the plugin version, so a release always fetches fresh files.
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(stream, contentType);
    }
}
