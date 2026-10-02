using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Cinematheque.Catalog;
using Jellyfin.Plugin.Cinematheque.Configuration;
using Jellyfin.Plugin.Cinematheque.Library;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Cinematheque.Api;

/// <summary>
/// The Cinematheque API: directors, actors, countries, movements and filtered film lists.
/// </summary>
[ApiController]
[Authorize]
[Route("Cinematheque")]
[Produces(MediaTypeNames.Application.Json)]
public class CinemathequeController : ControllerBase
{
    // Same claim Jellyfin's own controllers read the user from.
    private const string UserIdClaim = "Jellyfin-UserId";
    private const int MaxPageSize = 500;

    private readonly CatalogProvider _catalogProvider;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CinemathequeController"/> class.
    /// </summary>
    /// <param name="catalogProvider">The catalog provider.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    public CinemathequeController(CatalogProvider catalogProvider, IUserManager userManager, ILibraryManager libraryManager)
    {
        _catalogProvider = catalogProvider;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>
    /// Lists the directors in the user's films.
    /// </summary>
    /// <param name="search">Only names containing this text.</param>
    /// <param name="minFilms">The minimum number of films. Defaults to the configured value.</param>
    /// <param name="sortBy">Either <c>count</c> (default) or <c>name</c>.</param>
    /// <param name="startIndex">The index of the first result.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>A page of directors.</returns>
    [HttpGet("Directors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PageDto<PersonDto>> GetDirectors(
        [FromQuery] string? search,
        [FromQuery] int? minFilms,
        [FromQuery] string? sortBy,
        [FromQuery, Range(0, int.MaxValue)] int startIndex = 0,
        [FromQuery, Range(1, MaxPageSize)] int limit = 100)
        => GetPeople(PersonRole.Director, search, minFilms ?? Configuration.MinDirectorFilms, sortBy, startIndex, limit);

    /// <summary>
    /// Lists the actors in the user's films.
    /// </summary>
    /// <param name="search">Only names containing this text.</param>
    /// <param name="minFilms">The minimum number of films. Defaults to the configured value.</param>
    /// <param name="sortBy">Either <c>count</c> (default) or <c>name</c>.</param>
    /// <param name="startIndex">The index of the first result.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>A page of actors.</returns>
    [HttpGet("Actors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PageDto<PersonDto>> GetActors(
        [FromQuery] string? search,
        [FromQuery] int? minFilms,
        [FromQuery] string? sortBy,
        [FromQuery, Range(0, int.MaxValue)] int startIndex = 0,
        [FromQuery, Range(1, MaxPageSize)] int limit = 100)
        => GetPeople(PersonRole.Actor, search, minFilms ?? Configuration.MinActorFilms, sortBy, startIndex, limit);

    /// <summary>
    /// Lists the production countries in the user's films.
    /// </summary>
    /// <returns>The countries, most represented first.</returns>
    [HttpGet("Countries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CountrySummaryDto>> GetCountries()
    {
        User? user = GetUser();
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(_catalogProvider.GetCatalog(user).GetCountries()
            .Select(c => new CountrySummaryDto(
                c.Country.Code,
                c.Country.Name,
                c.FilmCount,
                c.Decades.Select(d => new DecadeDto(d.Decade, d.FilmCount)).ToArray(),
                c.Directors))
            .ToArray());
    }

    /// <summary>
    /// Lists the configured movements.
    /// </summary>
    /// <returns>The movements, in configuration order.</returns>
    [HttpGet("Movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MovementDto>> GetMovements()
    {
        User? user = GetUser();
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(_catalogProvider.GetCatalog(user).GetMovements()
            .Select(m => new MovementDto(
                m.Movement.Id,
                m.Movement.Name,
                m.Movement.Description,
                m.Movement.YearFrom,
                m.Movement.YearTo,
                Catalog.Country.FromLocations(m.Movement.Countries).Select(ToDto).ToArray(),
                m.FilmCount))
            .ToArray());
    }

    /// <summary>
    /// Gets the built-in movements, for the configuration page's reset button.
    /// </summary>
    /// <returns>The default movements.</returns>
    [HttpGet("Movements/Defaults")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MovementDefinition>> GetDefaultMovements()
        => Ok(DefaultMovements.Create());

    /// <summary>
    /// Lists films, narrowed by any combination of filters.
    /// </summary>
    /// <param name="country">A country code.</param>
    /// <param name="director">A director name.</param>
    /// <param name="actor">An actor name.</param>
    /// <param name="movement">A movement id.</param>
    /// <param name="decade">A decade, such as 1960.</param>
    /// <param name="startIndex">The index of the first result.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>A page of films, oldest first, with the decades available for further filtering.</returns>
    [HttpGet("Films")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<FilmPageDto> GetFilms(
        [FromQuery] string? country,
        [FromQuery] string? director,
        [FromQuery] string? actor,
        [FromQuery] string? movement,
        [FromQuery] int? decade,
        [FromQuery, Range(0, int.MaxValue)] int startIndex = 0,
        [FromQuery, Range(1, MaxPageSize)] int limit = 100)
    {
        User? user = GetUser();
        if (user is null)
        {
            return Unauthorized();
        }

        FilmCatalog catalog = _catalogProvider.GetCatalog(user);

        // Decades are computed before the decade filter so the client can switch between them.
        Film[] unfiltered = catalog.Filter(new FilmFilter(country, director, actor, movement)).ToArray();
        DecadeDto[] decades = unfiltered
            .Where(f => f.Decade is not null)
            .GroupBy(f => f.Decade!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new DecadeDto(g.Key, g.Count()))
            .ToArray();
        Film[] films = decade is null ? unfiltered : unfiltered.Where(f => f.Decade == decade).ToArray();

        return Ok(new FilmPageDto(
            films.Skip(startIndex).Take(limit)
                .Select(f => new FilmDto(f.Id, f.Name, f.Year, f.Countries.Select(ToDto).ToArray(), f.Directors))
                .ToArray(),
            films.Length,
            decades));
    }

    private static CountryDto ToDto(Catalog.Country country) => new CountryDto(country.Code, country.Name);

    private ActionResult<PageDto<PersonDto>> GetPeople(PersonRole role, string? search, int minFilms, string? sortBy, int startIndex, int limit)
    {
        User? user = GetUser();
        if (user is null)
        {
            return Unauthorized();
        }

        IEnumerable<PersonSummary> people = _catalogProvider.GetCatalog(user).GetPeople(role)
            .Where(p => p.FilmCount >= minFilms);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string key = Names.Key(search);
            people = people.Where(p => Names.Key(p.Name).Contains(key, StringComparison.Ordinal));
        }

        if (string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase))
        {
            people = people.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
        }

        PersonSummary[] all = people.ToArray();
        return Ok(new PageDto<PersonDto>(
            all.Skip(startIndex).Take(limit)
                .Select(p => new PersonDto(
                    _libraryManager.GetPersonId(p.Name),
                    p.Name,
                    p.FilmCount,
                    p.FirstYear,
                    p.LastYear,
                    p.Countries.Select(ToDto).ToArray()))
                .ToArray(),
            all.Length));
    }

    private User? GetUser()
    {
        string? value = User.FindFirst(UserIdClaim)?.Value;
        return Guid.TryParse(value, out Guid userId) ? _userManager.GetUserById(userId) : null;
    }
}
