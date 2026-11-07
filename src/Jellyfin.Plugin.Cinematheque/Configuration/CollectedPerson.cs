using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Cinematheque.Catalog;

namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// A person the administrator chose to mirror into a Jellyfin collection, in one role.
/// </summary>
public class CollectedPerson
{
    /// <summary>
    /// The prefix of the collection keys of people, before the role.
    /// </summary>
    public const string KeyPrefix = "person:";

    /// <summary>
    /// Gets or sets the role whose films make the collection.
    /// </summary>
    public PersonRole Role { get; set; }

    /// <summary>
    /// Gets or sets the person, in a form <see cref="PersonRef.Parse"/> accepts, such as
    /// <c>Akira Kurosawa (tmdb:5026)</c>. The name keeps the collection working when a credit
    /// has no TMDB id.
    /// </summary>
    public string Person { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name the person was added under, shown while they have no films.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the person key, as in <see cref="Credit.Key"/>: <c>tmdb:5026</c>, or the name key
    /// when there is no TMDB id. Empty when <see cref="Person"/> names nobody.
    /// </summary>
    public string PersonKey
    {
        get
        {
            PersonRef? person = PersonRef.Parse(Person);
            if (person is null)
            {
                return string.Empty;
            }

            return person.TmdbId is not null
                ? PersonRef.TmdbPrefix + person.TmdbId
                : PersonRef.NamePrefix + person.NameKey;
        }
    }

    /// <summary>
    /// Gets the identity of the person in this role, such as <c>director:tmdb:5026</c>. Empty
    /// when <see cref="Person"/> names nobody.
    /// </summary>
    public string Identity
    {
        get
        {
            string key = PersonKey;
            return key.Length == 0 ? string.Empty : Role.ToString().ToLowerInvariant() + ":" + key;
        }
    }

    /// <summary>
    /// Gets the key of the collection in <see cref="PluginConfiguration.ManagedCollections"/>.
    /// </summary>
    public string CollectionKey => KeyPrefix + Identity;

    /// <summary>
    /// Builds the entry for a person of the library.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="person">The person.</param>
    /// <returns>The entry.</returns>
    public static CollectedPerson For(PersonRole role, PersonSummary person)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new CollectedPerson
        {
            Role = role,
            Person = person.TmdbId is null ? person.Name : person.Name + " (" + PersonRef.TmdbPrefix + person.TmdbId + ")",
            Name = person.Name,
        };
    }

    /// <summary>
    /// Finds the person in a catalog, never a namesake.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <returns>The person, or <c>null</c> when they have no films in this role or cannot be
    /// told apart from a namesake.</returns>
    /// <remarks>
    /// <see cref="FilmCatalog.FindPerson"/> settles for the most prolific namesake, which suits a
    /// link but would fill a collection with someone else's films. Here the key must match. The
    /// one exception is a person added by name who has gained a TMDB id since: their key changed,
    /// so the name is accepted when a single person in the role goes by it.
    /// </remarks>
    public PersonSummary? Resolve(FilmCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        PersonRef? person = PersonRef.Parse(Person);
        if (person is null)
        {
            return null;
        }

        string key = PersonKey;
        IReadOnlyList<PersonSummary> people = catalog.GetPeople(Role);
        PersonSummary? exact = people.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.Ordinal));
        if (exact is not null || person.TmdbId is not null)
        {
            return exact;
        }

        PersonSummary[] sameName = people
            .Where(p => p.Spellings.Any(s => string.Equals(Names.Key(s), person.NameKey, StringComparison.Ordinal)))
            .Take(2)
            .ToArray();
        return sameName.Length == 1 ? sameName[0] : null;
    }

    /// <summary>
    /// Names the collection, in the server's display language.
    /// </summary>
    /// <param name="name">The person's name.</param>
    /// <param name="language">A language code such as <c>fr</c>.</param>
    /// <returns>The collection name, such as "Directed by Akira Kurosawa".</returns>
    /// <remarks>
    /// The role is part of the name: a collection called after the person alone would read like
    /// their person page, and Clint Eastwood the director and the actor need two names.
    /// </remarks>
    public string CollectionName(string name, string? language)
    {
        bool french = language?.Split('-', '_')[0].Equals("fr", StringComparison.OrdinalIgnoreCase) ?? false;
        string format = Role switch
        {
            PersonRole.Actor => french ? "Avec {0}" : "Starring {0}",
            PersonRole.Writer => french ? "Écrit par {0}" : "Written by {0}",
            _ => french ? "Réalisé par {0}" : "Directed by {0}",
        };
        return string.Format(CultureInfo.InvariantCulture, format, name);
    }
}
