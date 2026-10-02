using System.Globalization;
using System.Text;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// Name comparison that survives the usual metadata noise.
/// </summary>
/// <remarks>
/// "Nagisa Ōshima", "Nagisa Oshima" and "NAGISA OSHIMA" are the same director, and so are
/// "F.W. Murnau" and "F. W. Murnau". Keys keep letters and digits only, without diacritics.
/// </remarks>
public static class Names
{
    /// <summary>
    /// Builds the comparison key for a person, country or genre name.
    /// </summary>
    /// <param name="value">The name as found in metadata or configuration.</param>
    /// <returns>A lowercase key with diacritics, spaces and punctuation removed.</returns>
    public static string Key(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a URL-friendly slug, used for countries that have no ISO code.
    /// </summary>
    /// <param name="value">The name to turn into a slug.</param>
    /// <returns>A lowercase slug made of ASCII letters, digits and dashes.</returns>
    public static string Slug(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        bool pendingDash = false;
        foreach (char c in decomposed)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
                pendingDash = false;
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                pendingDash = true;
            }
        }

        return builder.ToString();
    }
}
