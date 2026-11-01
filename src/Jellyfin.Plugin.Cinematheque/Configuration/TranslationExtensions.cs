using System;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// Picks a translation for a language.
/// </summary>
internal static class TranslationExtensions
{
    /// <summary>
    /// Finds the translation for a language, or for its base language.
    /// </summary>
    /// <typeparam name="T">The translation type.</typeparam>
    /// <param name="translations">The translations, which may be missing from a saved configuration.</param>
    /// <param name="language">A language code such as <c>fr</c> or <c>fr-CA</c>.</param>
    /// <param name="languageOf">Reads the language of a translation.</param>
    /// <returns>The translation, or <c>null</c> when there is none.</returns>
    internal static T? FindForLanguage<T>(this T[]? translations, string? language, Func<T, string> languageOf)
        where T : class
    {
        if (translations is null || string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        string baseLanguage = language.Split('-', '_')[0];
        return Array.Find(translations, t => string.Equals(languageOf(t), language, StringComparison.OrdinalIgnoreCase))
            ?? Array.Find(translations, t => string.Equals(languageOf(t), baseLanguage, StringComparison.OrdinalIgnoreCase));
    }
}
