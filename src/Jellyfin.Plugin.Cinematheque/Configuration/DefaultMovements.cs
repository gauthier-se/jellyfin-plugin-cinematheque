namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The movements a fresh install starts with. Administrators can edit or replace them.
/// </summary>
/// <remarks>
/// Each movement is a country, a period and the directors who defined it. That is narrower than
/// film history would allow, and deliberately so: a rule that only says "Italy, 1945 to 1955"
/// sweeps in every comedy of the period. Directors carry their TMDB id, taken from Wikidata
/// (property P4985), so they match whatever script their credits use; the name is the fallback for
/// credits without an id, compared without diacritics or punctuation.
/// </remarks>
public static class DefaultMovements
{
    /// <summary>
    /// Builds a new copy of the default movements.
    /// </summary>
    /// <returns>The default movements.</returns>
    public static MovementDefinition[] Create() =>
    [
        MovementDefinition.Create(
            "german-expressionism",
            "German Expressionism",
            "Painted shadows, distorted sets and the birth of horror in Weimar cinema.",
            ["DE"],
            (1919, 1933),
            ["Robert Wiene (tmdb:2991)", "F. W. Murnau (tmdb:9076)", "Fritz Lang (tmdb:68)", "Paul Leni (tmdb:48055)", "Paul Wegener (tmdb:29123)", "Karl Grune (tmdb:135425)", "Arthur Robison (tmdb:48049)", "Henrik Galeen (tmdb:9833)"]),
        MovementDefinition.Create(
            "italian-neorealism",
            "Italian Neorealism",
            "Non-professional actors and real streets in post-war Italy.",
            ["IT"],
            (1943, 1955),
            ["Roberto Rossellini (tmdb:4410)", "Vittorio De Sica (tmdb:12329)", "Luchino Visconti (tmdb:15127)", "Giuseppe De Santis (tmdb:41634)", "Alberto Lattuada (tmdb:146793)", "Pietro Germi (tmdb:95040)", "Luigi Zampa (tmdb:31890)", "Renato Castellani (tmdb:20877)"]),
        MovementDefinition.Create(
            "french-new-wave",
            "French New Wave",
            "Handheld cameras, jump cuts and the critics of Cahiers du cinéma behind the camera.",
            ["FR"],
            (1958, 1973),
            ["Jean-Luc Godard (tmdb:3776)", "François Truffaut (tmdb:1650)", "Claude Chabrol (tmdb:19069)", "Éric Rohmer (tmdb:28615)", "Jacques Rivette (tmdb:73153)", "Agnès Varda (tmdb:6817)", "Alain Resnais (tmdb:11983)", "Jacques Demy (tmdb:24882)", "Louis Malle (tmdb:15389)", "Chris Marker (tmdb:9956)", "Jacques Rozier (tmdb:237666)", "Jean Eustache (tmdb:53914)"]),
        MovementDefinition.Create(
            "japanese-new-wave",
            "Japanese New Wave",
            "Radical politics, sex and violence against the studio system.",
            ["JP"],
            (1956, 1975),
            ["Nagisa Oshima (tmdb:46230)", "Shohei Imamura (tmdb:20025)", "Masahiro Shinoda (tmdb:133518)", "Yoshishige Yoshida (tmdb:110519)", "Kiju Yoshida (tmdb:110519)", "Hiroshi Teshigahara (tmdb:96801)", "Seijun Suzuki (tmdb:82461)", "Susumu Hani (tmdb:1041699)", "Toshio Matsumoto (tmdb:17542)", "Koji Wakamatsu (tmdb:134811)"]),
        MovementDefinition.Create(
            "czechoslovak-new-wave",
            "Czechoslovak New Wave",
            "Absurd humour and quiet defiance before the Prague Spring was crushed.",
            ["XC", "CZ", "SK"],
            (1962, 1970),
            ["Miloš Forman (tmdb:3974)", "Věra Chytilová (tmdb:124137)", "Jiří Menzel (tmdb:11720)", "Jan Němec (tmdb:1024901)", "Ivan Passer (tmdb:84736)", "Juraj Herz (tmdb:83998)", "Ján Kadár (tmdb:544700)", "Elmar Klos (tmdb:544699)", "Juraj Jakubisko (tmdb:78001)", "František Vláčil (tmdb:226662)", "Pavel Juráček (tmdb:119999)"]),
        MovementDefinition.Create(
            "cinema-novo",
            "Cinema Novo",
            "\"A camera in hand and an idea in the head\": Brazil's political cinema.",
            ["BR"],
            (1960, 1972),
            ["Glauber Rocha (tmdb:544845)", "Nelson Pereira dos Santos (tmdb:12386)", "Ruy Guerra (tmdb:20548)", "Carlos Diegues (tmdb:112117)", "Joaquim Pedro de Andrade (tmdb:254884)", "Leon Hirszman (tmdb:1185869)"]),
        MovementDefinition.Create(
            "spaghetti-western",
            "Spaghetti Western",
            "The Italian reinvention of the western, shot in Almería and scored by Morricone.",
            ["IT"],
            (1960, 1978),
            [],
            ["Western"]),
        MovementDefinition.Create(
            "new-hollywood",
            "New Hollywood",
            "Directors take over the studios, from Bonnie and Clyde to Heaven's Gate.",
            ["US"],
            (1967, 1980),
            ["Arthur Penn (tmdb:6448)", "Mike Nichols (tmdb:5342)", "Dennis Hopper (tmdb:2778)", "Robert Altman (tmdb:9789)", "Francis Ford Coppola (tmdb:1776)", "Martin Scorsese (tmdb:1032)", "Hal Ashby (tmdb:4964)", "Peter Bogdanovich (tmdb:39012)", "William Friedkin (tmdb:15175)", "Bob Rafelson (tmdb:19450)", "Terrence Malick (tmdb:30715)", "Brian De Palma (tmdb:1150)", "Michael Cimino (tmdb:12114)", "Monte Hellman (tmdb:20921)", "Alan J. Pakula (tmdb:6349)", "Sidney Lumet (tmdb:39996)"]),
        MovementDefinition.Create(
            "new-german-cinema",
            "New German Cinema",
            "The Oberhausen generation confronting post-war Germany.",
            ["DE"],
            (1962, 1982),
            ["Rainer Werner Fassbinder (tmdb:2725)", "Werner Herzog (tmdb:6818)", "Wim Wenders (tmdb:2303)", "Volker Schlöndorff (tmdb:10249)", "Margarethe von Trotta (tmdb:39298)", "Alexander Kluge (tmdb:50870)", "Hans-Jürgen Syberberg (tmdb:227824)", "Werner Schroeter (tmdb:45192)", "Edgar Reitz (tmdb:72908)"]),
        MovementDefinition.Create(
            "hong-kong-new-wave",
            "Hong Kong New Wave",
            "Television-trained directors who remade Hong Kong cinema, and the second wave that followed.",
            ["HK"],
            (1978, 1997),
            ["Tsui Hark (tmdb:26760)", "Ann Hui (tmdb:123199)", "Patrick Tam (tmdb:71063)", "Allen Fong (tmdb:975054)", "Yim Ho", "Alex Cheung", "Clara Law (tmdb:96449)", "Stanley Kwan (tmdb:96861)", "Wong Kar-wai (tmdb:12453)", "Fruit Chan (tmdb:56865)", "Mabel Cheung (tmdb:103602)"]),
        MovementDefinition.Create(
            "heroic-bloodshed",
            "Heroic Bloodshed",
            "Brotherhood, betrayal and balletic gunfights from Hong Kong's golden age.",
            ["HK"],
            (1986, 1998),
            ["John Woo (tmdb:11401)", "Ringo Lam (tmdb:26767)", "Kirk Wong (tmdb:58036)", "Johnnie To (tmdb:25236)", "Andrew Lau (tmdb:65994)", "Gordon Chan (tmdb:64901)"],
            ["Action", "Crime", "Thriller"]),
        MovementDefinition.Create(
            "taiwan-new-cinema",
            "Taiwan New Cinema",
            "Long takes and everyday life in a changing Taiwan.",
            ["TW"],
            (1982, 2000),
            ["Hou Hsiao-hsien (tmdb:64992)", "Edward Yang (tmdb:143035)", "Tsai Ming-liang (tmdb:71174)", "Wan Jen (tmdb:1158867)", "Wang Tung (tmdb:1158889)", "Ko I-chen"]),
        MovementDefinition.Create(
            "iranian-new-wave",
            "Iranian New Wave",
            "Poetic realism blurring fiction and documentary.",
            ["IR"],
            (1969, 2010),
            ["Abbas Kiarostami (tmdb:119294)", "Mohsen Makhmalbaf (tmdb:120226)", "Jafar Panahi (tmdb:120229)", "Dariush Mehrjui (tmdb:120227)", "Majid Majidi (tmdb:110695)", "Bahman Ghobadi (tmdb:54607)", "Samira Makhmalbaf (tmdb:20026)", "Bahram Beyzaie (tmdb:1098592)", "Sohrab Shahid-Saless (tmdb:1198165)"]),
    ];
}
