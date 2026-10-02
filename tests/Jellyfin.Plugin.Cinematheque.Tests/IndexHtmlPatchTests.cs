using Jellyfin.Plugin.Cinematheque.Integration;

namespace Jellyfin.Plugin.Cinematheque.Tests;

public class IndexHtmlPatchTests
{
    private const string Html = "<html><head></head><body><div id=\"reactRoot\"></div></body></html>";

    [Fact]
    public void Apply_adds_the_loader_before_the_closing_body_tag()
    {
        string patched = IndexHtmlPatch.Apply(new TransformationPayload { Contents = Html });

        int script = patched.IndexOf("../Cinematheque/Web/loader.js", StringComparison.Ordinal);
        Assert.True(script > 0);
        Assert.True(script < patched.IndexOf("</body>", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_is_idempotent()
    {
        string once = IndexHtmlPatch.Apply(new TransformationPayload { Contents = Html });
        string twice = IndexHtmlPatch.Apply(new TransformationPayload { Contents = once });

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Apply_leaves_documents_without_a_body_untouched()
        => Assert.Equal("<div></div>", IndexHtmlPatch.Apply(new TransformationPayload { Contents = "<div></div>" }));
}
