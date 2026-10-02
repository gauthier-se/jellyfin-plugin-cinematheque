namespace Jellyfin.Plugin.Cinematheque.Integration;

/// <summary>
/// The argument File Transformation passes to a callback, deserialized from <c>{ "contents": "..." }</c>.
/// </summary>
public class TransformationPayload
{
    /// <summary>
    /// Gets or sets the current contents of the file being served.
    /// </summary>
    public string? Contents { get; set; }
}
