namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The movements a fresh install starts with. Administrators can edit or replace them.
/// </summary>
/// <remarks>
/// Each movement is a country, a period and the directors who defined it. That is narrower than
/// film history would allow, and deliberately so: a rule that only says "Italy, 1945 to 1955"
/// sweeps in every comedy of the period. Names are matched without diacritics or punctuation, so
/// "Nagisa Oshima" also matches "Nagisa Ōshima".
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
            ["Robert Wiene", "F. W. Murnau", "Fritz Lang", "Paul Leni", "Paul Wegener", "Karl Grune", "Arthur Robison", "Henrik Galeen"]),
        MovementDefinition.Create(
            "italian-neorealism",
            "Italian Neorealism",
            "Non-professional actors and real streets in post-war Italy.",
            ["IT"],
            (1943, 1955),
            ["Roberto Rossellini", "Vittorio De Sica", "Luchino Visconti", "Giuseppe De Santis", "Alberto Lattuada", "Pietro Germi", "Luigi Zampa", "Renato Castellani"]),
        MovementDefinition.Create(
            "french-new-wave",
            "French New Wave",
            "Handheld cameras, jump cuts and the critics of Cahiers du cinéma behind the camera.",
            ["FR"],
            (1958, 1973),
            ["Jean-Luc Godard", "François Truffaut", "Claude Chabrol", "Éric Rohmer", "Jacques Rivette", "Agnès Varda", "Alain Resnais", "Jacques Demy", "Louis Malle", "Chris Marker", "Jacques Rozier", "Jean Eustache"]),
        MovementDefinition.Create(
            "japanese-new-wave",
            "Japanese New Wave",
            "Radical politics, sex and violence against the studio system.",
            ["JP"],
            (1956, 1975),
            ["Nagisa Oshima", "Shohei Imamura", "Masahiro Shinoda", "Yoshishige Yoshida", "Kiju Yoshida", "Hiroshi Teshigahara", "Seijun Suzuki", "Susumu Hani", "Toshio Matsumoto", "Koji Wakamatsu"]),
        MovementDefinition.Create(
            "czechoslovak-new-wave",
            "Czechoslovak New Wave",
            "Absurd humour and quiet defiance before the Prague Spring was crushed.",
            ["XC", "CZ", "SK"],
            (1962, 1970),
            ["Miloš Forman", "Věra Chytilová", "Jiří Menzel", "Jan Němec", "Ivan Passer", "Juraj Herz", "Ján Kadár", "Elmar Klos", "Juraj Jakubisko", "František Vláčil", "Pavel Juráček"]),
        MovementDefinition.Create(
            "cinema-novo",
            "Cinema Novo",
            "\"A camera in hand and an idea in the head\": Brazil's political cinema.",
            ["BR"],
            (1960, 1972),
            ["Glauber Rocha", "Nelson Pereira dos Santos", "Ruy Guerra", "Carlos Diegues", "Joaquim Pedro de Andrade", "Leon Hirszman"]),
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
            ["Arthur Penn", "Mike Nichols", "Dennis Hopper", "Robert Altman", "Francis Ford Coppola", "Martin Scorsese", "Hal Ashby", "Peter Bogdanovich", "William Friedkin", "Bob Rafelson", "Terrence Malick", "Brian De Palma", "Michael Cimino", "Monte Hellman", "Alan J. Pakula", "Sidney Lumet"]),
        MovementDefinition.Create(
            "new-german-cinema",
            "New German Cinema",
            "The Oberhausen generation confronting post-war Germany.",
            ["DE"],
            (1962, 1982),
            ["Rainer Werner Fassbinder", "Werner Herzog", "Wim Wenders", "Volker Schlöndorff", "Margarethe von Trotta", "Alexander Kluge", "Hans-Jürgen Syberberg", "Werner Schroeter", "Edgar Reitz"]),
        MovementDefinition.Create(
            "hong-kong-new-wave",
            "Hong Kong New Wave",
            "Television-trained directors who remade Hong Kong cinema, and the second wave that followed.",
            ["HK"],
            (1978, 1997),
            ["Tsui Hark", "Ann Hui", "Patrick Tam", "Allen Fong", "Yim Ho", "Alex Cheung", "Clara Law", "Stanley Kwan", "Wong Kar-wai", "Fruit Chan", "Mabel Cheung"]),
        MovementDefinition.Create(
            "heroic-bloodshed",
            "Heroic Bloodshed",
            "Brotherhood, betrayal and balletic gunfights from Hong Kong's golden age.",
            ["HK"],
            (1986, 1998),
            ["John Woo", "Ringo Lam", "Kirk Wong", "Johnnie To", "Andrew Lau", "Gordon Chan"],
            ["Action", "Crime", "Thriller"]),
        MovementDefinition.Create(
            "taiwan-new-cinema",
            "Taiwan New Cinema",
            "Long takes and everyday life in a changing Taiwan.",
            ["TW"],
            (1982, 2000),
            ["Hou Hsiao-hsien", "Edward Yang", "Tsai Ming-liang", "Wan Jen", "Wang Tung", "Ko I-chen"]),
        MovementDefinition.Create(
            "iranian-new-wave",
            "Iranian New Wave",
            "Poetic realism blurring fiction and documentary.",
            ["IR"],
            (1969, 2010),
            ["Abbas Kiarostami", "Mohsen Makhmalbaf", "Jafar Panahi", "Dariush Mehrjui", "Majid Majidi", "Bahman Ghobadi", "Samira Makhmalbaf", "Bahram Beyzaie", "Sohrab Shahid-Saless"]),
    ];
}
