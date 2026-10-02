using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Cinematheque.Catalog;

/// <summary>
/// A production country.
/// </summary>
/// <param name="Code">
/// The ISO 3166-1 alpha-2 code for current countries, the code TMDB uses for historical ones
/// (SU, YU, XC, XG, CS), or a slug of the name for anything unrecognized.
/// </param>
/// <param name="Name">The English name, used when the client cannot localize the code.</param>
public sealed record Country(string Code, string Name)
{
    // English names as TMDB, NFO files and older scrapers write them, keyed by code. The first
    // name is the display name. Historical states stay separate on purpose: a Soviet film is not
    // a Russian film, and Czechoslovak cinema is a chapter of its own. West Germany is the
    // exception, folded into Germany the way TMDB does it.
    private static readonly (string Code, string[] Names)[] _known =
    [
        ("AF", ["Afghanistan"]),
        ("AL", ["Albania"]),
        ("DZ", ["Algeria"]),
        ("AR", ["Argentina"]),
        ("AM", ["Armenia"]),
        ("AU", ["Australia"]),
        ("AT", ["Austria"]),
        ("AZ", ["Azerbaijan"]),
        ("BD", ["Bangladesh"]),
        ("BY", ["Belarus"]),
        ("BE", ["Belgium"]),
        ("BT", ["Bhutan"]),
        ("BO", ["Bolivia"]),
        ("BA", ["Bosnia and Herzegovina"]),
        ("BR", ["Brazil"]),
        ("BG", ["Bulgaria"]),
        ("BF", ["Burkina Faso"]),
        ("KH", ["Cambodia"]),
        ("CM", ["Cameroon"]),
        ("CA", ["Canada"]),
        ("TD", ["Chad"]),
        ("CL", ["Chile"]),
        ("CN", ["China", "People's Republic of China", "PRC"]),
        ("CO", ["Colombia"]),
        ("CR", ["Costa Rica"]),
        ("CI", ["Ivory Coast", "Cote D'Ivoire", "Côte d'Ivoire"]),
        ("HR", ["Croatia"]),
        ("CU", ["Cuba"]),
        ("CY", ["Cyprus"]),
        ("CZ", ["Czech Republic", "Czechia"]),
        ("DK", ["Denmark"]),
        ("DO", ["Dominican Republic"]),
        ("EC", ["Ecuador"]),
        ("EG", ["Egypt"]),
        ("EE", ["Estonia"]),
        ("ET", ["Ethiopia"]),
        ("FI", ["Finland"]),
        ("FR", ["France"]),
        ("GE", ["Georgia"]),
        ("DE", ["Germany", "West Germany", "Federal Republic of Germany", "FRG"]),
        ("GH", ["Ghana"]),
        ("GR", ["Greece"]),
        ("GT", ["Guatemala"]),
        ("HK", ["Hong Kong", "Hong Kong SAR", "Hong Kong SAR China"]),
        ("HU", ["Hungary"]),
        ("IS", ["Iceland"]),
        ("IN", ["India"]),
        ("ID", ["Indonesia"]),
        ("IR", ["Iran", "Islamic Republic of Iran"]),
        ("IQ", ["Iraq"]),
        ("IE", ["Ireland"]),
        ("IL", ["Israel"]),
        ("IT", ["Italy"]),
        ("JM", ["Jamaica"]),
        ("JP", ["Japan"]),
        ("JO", ["Jordan"]),
        ("KZ", ["Kazakhstan"]),
        ("KE", ["Kenya"]),
        ("XK", ["Kosovo"]),
        ("KG", ["Kyrgyzstan"]),
        ("LA", ["Laos"]),
        ("LV", ["Latvia"]),
        ("LB", ["Lebanon"]),
        ("LT", ["Lithuania"]),
        ("LU", ["Luxembourg"]),
        ("MO", ["Macao", "Macau"]),
        ("MY", ["Malaysia"]),
        ("ML", ["Mali"]),
        ("MT", ["Malta"]),
        ("MR", ["Mauritania"]),
        ("MX", ["Mexico"]),
        ("MD", ["Moldova"]),
        ("MC", ["Monaco"]),
        ("MN", ["Mongolia"]),
        ("ME", ["Montenegro"]),
        ("MA", ["Morocco"]),
        ("MM", ["Myanmar", "Burma"]),
        ("NP", ["Nepal"]),
        ("NL", ["Netherlands", "The Netherlands", "Holland"]),
        ("NZ", ["New Zealand"]),
        ("NG", ["Nigeria"]),
        ("KP", ["North Korea", "Democratic People's Republic of Korea"]),
        ("MK", ["North Macedonia", "Macedonia"]),
        ("NO", ["Norway"]),
        ("PK", ["Pakistan"]),
        ("PS", ["Palestine", "Palestinian Territory", "State of Palestine"]),
        ("PA", ["Panama"]),
        ("PY", ["Paraguay"]),
        ("PE", ["Peru"]),
        ("PH", ["Philippines"]),
        ("PL", ["Poland"]),
        ("PT", ["Portugal"]),
        ("PR", ["Puerto Rico"]),
        ("QA", ["Qatar"]),
        ("RO", ["Romania"]),
        ("RU", ["Russia", "Russian Federation"]),
        ("RW", ["Rwanda"]),
        ("SA", ["Saudi Arabia"]),
        ("SN", ["Senegal"]),
        ("RS", ["Serbia"]),
        ("SG", ["Singapore"]),
        ("SK", ["Slovakia"]),
        ("SI", ["Slovenia"]),
        ("ZA", ["South Africa"]),
        ("KR", ["South Korea", "Korea", "Republic of Korea", "Korea, Republic of"]),
        ("ES", ["Spain"]),
        ("LK", ["Sri Lanka"]),
        ("SE", ["Sweden"]),
        ("CH", ["Switzerland"]),
        ("SY", ["Syria", "Syrian Arab Republic"]),
        ("TW", ["Taiwan", "Republic of China"]),
        ("TJ", ["Tajikistan"]),
        ("TZ", ["Tanzania"]),
        ("TH", ["Thailand"]),
        ("TN", ["Tunisia"]),
        ("TR", ["Turkey", "Türkiye"]),
        ("UA", ["Ukraine"]),
        ("AE", ["United Arab Emirates", "UAE"]),
        ("GB", ["United Kingdom", "UK", "Great Britain", "England", "Scotland", "Wales"]),
        ("US", ["United States of America", "United States", "USA", "US", "America"]),
        ("UY", ["Uruguay"]),
        ("UZ", ["Uzbekistan"]),
        ("VE", ["Venezuela"]),
        ("VN", ["Vietnam", "Viet Nam"]),
        ("ZW", ["Zimbabwe"]),

        // Historical states, with the codes TMDB uses for them.
        ("SU", ["Soviet Union", "USSR", "Union of Soviet Socialist Republics"]),
        ("YU", ["Yugoslavia"]),
        ("XC", ["Czechoslovakia"]),
        ("XG", ["East Germany", "German Democratic Republic", "GDR"]),
        ("CS", ["Serbia and Montenegro"]),
    ];

    private static readonly FrozenDictionary<string, Country> _byKey = BuildIndex();

    /// <summary>
    /// Resolves a production location as stored by Jellyfin into a country.
    /// </summary>
    /// <param name="location">The raw value, such as "Hong Kong" or "United States of America".</param>
    /// <returns>The country, or <c>null</c> when the value is blank.</returns>
    public static Country? FromLocation(string? location)
    {
        string key = Names.Key(location);
        if (key.Length == 0)
        {
            return null;
        }

        if (_byKey.TryGetValue(key, out Country? country))
        {
            return country;
        }

        string name = location!.Trim();
        return new Country(Names.Slug(name), name);
    }

    /// <summary>
    /// Resolves the production locations of one film, dropping blanks and duplicates.
    /// </summary>
    /// <param name="locations">The raw values.</param>
    /// <returns>The distinct countries, in their original order.</returns>
    public static IReadOnlyList<Country> FromLocations(IEnumerable<string>? locations)
    {
        if (locations is null)
        {
            return [];
        }

        List<Country> result = [];
        foreach (string location in locations)
        {
            Country? country = FromLocation(location);
            if (country is not null && !result.Exists(c => string.Equals(c.Code, country.Code, StringComparison.Ordinal)))
            {
                result.Add(country);
            }
        }

        return result;
    }

    private static FrozenDictionary<string, Country> BuildIndex()
    {
        Dictionary<string, Country> index = new Dictionary<string, Country>(StringComparer.Ordinal);
        foreach ((string code, string[] names) in _known)
        {
            Country country = new Country(code, names[0]);
            index[Names.Key(code)] = country;
            foreach (string name in names)
            {
                index[Names.Key(name)] = country;
            }
        }

        return index.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
