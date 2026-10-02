using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// One page of results.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="TotalRecordCount">The number of items across all pages.</param>
public sealed record PageDto<T>(IReadOnlyList<T> Items, int TotalRecordCount);
