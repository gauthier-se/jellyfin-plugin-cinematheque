using System;
using System.Globalization;

namespace Jellyfin.Plugin.Cinematheque.Integration;

/// <summary>
/// The File Transformation callback that adds the Cinematheque loader to jellyfin-web's index.html.
/// </summary>
/// <remarks>
/// File Transformation finds this class and method by name through reflection, so renaming either
/// breaks the integration silently. The patch stays a single script tag: everything else lives in
/// the served files, which keeps it independent of how jellyfin-web is built.
/// </remarks>
public static class IndexHtmlPatch
{
    private const string Marker = "data-cinematheque";

    /// <summary>
    /// Adds the loader script before the closing body tag.
    /// </summary>
    /// <param name="payload">The file contents, as sent by File Transformation.</param>
    /// <returns>The patched contents.</returns>
    public static string Apply(TransformationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string contents = payload.Contents ?? string.Empty;
        if (contents.Contains(Marker, StringComparison.Ordinal))
        {
            return contents;
        }

        int index = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return contents;
        }

        // index.html is served from /web/, so the relative path also works behind a base URL.
        string script = string.Format(
            CultureInfo.InvariantCulture,
            "<script defer src=\"../Cinematheque/Web/loader.js?v={0}\" {1}></script>",
            typeof(IndexHtmlPatch).Assembly.GetName().Version,
            Marker);
        return contents.Insert(index, script);
    }
}
