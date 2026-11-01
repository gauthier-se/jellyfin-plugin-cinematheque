using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The film lists of the built-in movements, read from <c>MovementFilms.json</c>.
/// </summary>
/// <remarks>
/// They live in a data file rather than in <see cref="DefaultMovements"/> because they run to
/// hundreds of entries with titles in two languages, and because film noir's is generated.
/// </remarks>
internal static class MovementFilms
{
    private const string ResourceName = "Jellyfin.Plugin.Cinematheque.Configuration.MovementFilms.json";

    private static readonly Lazy<Dictionary<string, FilmList>> _lists = new(Load);

    /// <summary>
    /// Adds their film lists to movements.
    /// </summary>
    /// <param name="movements">The movements.</param>
    /// <returns>The same movements.</returns>
    internal static MovementDefinition[] AddTo(MovementDefinition[] movements)
    {
        foreach (MovementDefinition movement in movements)
        {
            if (_lists.Value.TryGetValue(movement.Id, out FilmList? list))
            {
                movement.Essentials = [.. list.Essentials];
                movement.TmdbIds = [.. list.TmdbIds];
                movement.ExcludedTmdbIds = [.. list.ExcludedTmdbIds];
            }
        }

        return movements;
    }

    private static Dictionary<string, FilmList> Load()
    {
        using Stream stream = typeof(MovementFilms).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource {ResourceName} is missing.");
        return JsonSerializer.Deserialize<Dictionary<string, FilmList>>(stream)
            ?? throw new InvalidOperationException($"The embedded resource {ResourceName} is empty.");
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Created by the JSON deserializer.")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "A read-only copy of a data file.")]
    private sealed class FilmList
    {
        public EssentialFilm[] Essentials { get; set; } = [];

        public string[] TmdbIds { get; set; } = [];

        public string[] ExcludedTmdbIds { get; set; } = [];
    }
}
