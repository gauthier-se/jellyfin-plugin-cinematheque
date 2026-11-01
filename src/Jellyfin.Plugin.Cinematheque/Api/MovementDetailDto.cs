using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// One movement, with what its page shows beyond the list.
/// </summary>
/// <param name="Movement">The movement.</param>
/// <param name="Directors">The directors with the most films in the movement.</param>
/// <param name="EssentialCount">How many essential films the movement lists.</param>
/// <param name="MissingEssentials">The essential films the user does not have.</param>
public sealed record MovementDetailDto(
    MovementDto Movement,
    IReadOnlyList<PersonLinkDto> Directors,
    int EssentialCount,
    IReadOnlyList<EssentialFilmDto> MissingEssentials);
