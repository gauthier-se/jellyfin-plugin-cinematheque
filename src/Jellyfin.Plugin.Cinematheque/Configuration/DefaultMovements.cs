namespace Jellyfin.Plugin.Cinematheque.Configuration;

/// <summary>
/// The movements a fresh install starts with. Administrators can edit or replace them.
/// </summary>
/// <remarks>
/// Most movements are a country, a period and the directors who defined it. That is narrower than
/// film history would allow, and deliberately so: a rule that only says "Italy, 1945 to 1955"
/// sweeps in every comedy of the period. A director only belongs here when their work of the period
/// belongs to the movement; one who touched it with a single film gets that film in
/// <c>MovementFilms.json</c> instead. That file also lists each movement's essential films, the
/// false positives to leave out, and the whole of the movements no director list can describe,
/// such as film noir, giallo and Dogme 95. Directors carry their TMDB id, taken from Wikidata
/// (property P4985), so they match whatever script their credits use; the name is the fallback for
/// credits without an id, compared without diacritics or punctuation.
/// </remarks>
public static class DefaultMovements
{
    /// <summary>
    /// Builds a new copy of the default movements.
    /// </summary>
    /// <returns>The default movements, by the year they started.</returns>
    public static MovementDefinition[] Create() => MovementFilms.AddTo(Rules());

    private static MovementDefinition[] Rules() =>
    [
        MovementDefinition.Create(
            "german-expressionism",
            "German Expressionism",
            "Painted shadows, distorted sets and the birth of horror in Weimar cinema.",
            ["DE"],
            (1919, 1933),
            ["Robert Wiene (tmdb:2991)", "F. W. Murnau (tmdb:9076)", "Fritz Lang (tmdb:68)", "Paul Leni (tmdb:48055)", "Paul Wegener (tmdb:29123)", "Karl Grune (tmdb:135425)", "Arthur Robison (tmdb:48049)", "Henrik Galeen (tmdb:9833)", "Lupu Pick (tmdb:28996)", "Leopold Jessner (tmdb:1093252)", "Karlheinz Martin (tmdb:591500)"])
            .WithTranslation("fr", "Expressionnisme allemand", "Ombres peintes, décors distordus et naissance du cinéma d'horreur sous Weimar."),
        MovementDefinition.Create(
            "soviet-montage",
            "Soviet Montage",
            "Eisenstein, Vertov and Pudovkin turn editing into a revolutionary argument.",
            ["SU"],
            (1924, 1931),
            ["Sergei Eisenstein (tmdb:9603)", "Vsevolod Pudovkin (tmdb:144821)", "Dziga Vertov (tmdb:95111)", "Alexander Dovzhenko (tmdb:141713)", "Lev Kuleshov (tmdb:228729)", "Esfir Shub (tmdb:1038944)", "Grigori Kozintsev (tmdb:562326)", "Leonid Trauberg (tmdb:1037667)"])
            .WithTranslation("fr", "Montage soviétique", "Eisenstein, Vertov et Poudovkine font du montage un argument révolutionnaire."),
        MovementDefinition.Create(
            "poetic-realism",
            "French Poetic Realism",
            "Doomed lovers, foggy quays and fatalism in 1930s France.",
            ["FR"],
            (1931, 1945),
            ["Marcel Carné (tmdb:25161)", "Jean Renoir (tmdb:11528)", "Julien Duvivier (tmdb:39058)", "Jean Grémillon (tmdb:238377)", "Jean Vigo (tmdb:142865)", "Pierre Chenal (tmdb:33797)"])
            .WithTranslation("fr", "Réalisme poétique français", "Amants maudits, quais brumeux et fatalité dans la France des années 1930."),
        MovementDefinition.Create(
            "film-noir",
            "Film Noir",
            "Shadows, femmes fatales and moral rot in the post-war crime film.",
            [],
            (1940, 1959),
            [])
            .WithTranslation("fr", "Film noir", "Ombres, femmes fatales et pourriture morale dans le polar de l'après-guerre."),
        MovementDefinition.Create(
            "italian-neorealism",
            "Italian Neorealism",
            "Non-professional actors and real streets in post-war Italy.",
            ["IT"],
            (1943, 1955),
            ["Roberto Rossellini (tmdb:4410)", "Vittorio De Sica (tmdb:12329)", "Luchino Visconti (tmdb:15127)", "Giuseppe De Santis (tmdb:41634)", "Alberto Lattuada (tmdb:146793)", "Pietro Germi (tmdb:95040)", "Luigi Zampa (tmdb:31890)", "Renato Castellani (tmdb:20877)", "Carlo Lizzani (tmdb:122016)", "Aldo Vergano (tmdb:1028248)", "Francesco De Robertis (tmdb:238469)"])
            .WithTranslation("fr", "Néoréalisme italien", "Acteurs non professionnels et vraies rues dans l'Italie d'après-guerre."),
        MovementDefinition.Create(
            "indian-parallel-cinema",
            "Indian Parallel Cinema",
            "Realism and social inquiry away from Bombay's commercial studios.",
            ["IN"],
            (1952, 1990),
            ["Satyajit Ray (tmdb:12160)", "Ritwik Ghatak (tmdb:543652)", "Mrinal Sen (tmdb:1086288)", "Shyam Benegal (tmdb:114636)", "Adoor Gopalakrishnan (tmdb:935843)", "Mani Kaul (tmdb:1087762)", "Kumar Shahani (tmdb:1296630)"])
            .WithTranslation("fr", "Cinéma parallèle indien", "Réalisme et regard social à l'écart des studios commerciaux de Bombay."),
        MovementDefinition.Create(
            "polish-film-school",
            "Polish Film School",
            "A generation reckons with war, occupation and heroism.",
            ["PL"],
            (1955, 1965),
            ["Andrzej Wajda (tmdb:2801)", "Andrzej Munk (tmdb:238433)", "Jerzy Kawalerowicz (tmdb:40615)", "Wojciech Has (tmdb:27002)", "Kazimierz Kutz (tmdb:591241)", "Tadeusz Konwicki (tmdb:1062920)"])
            .WithTranslation("fr", "École polonaise", "Une génération se confronte à la guerre, à l'occupation et à l'héroïsme."),
        MovementDefinition.Create(
            "japanese-new-wave",
            "Japanese New Wave",
            "Radical politics, sex and violence against the studio system.",
            ["JP"],
            (1956, 1975),
            ["Nagisa Oshima (tmdb:46230)", "Shohei Imamura (tmdb:20025)", "Masahiro Shinoda (tmdb:133518)", "Yoshishige Yoshida (tmdb:110519)", "Kiju Yoshida (tmdb:110519)", "Hiroshi Teshigahara (tmdb:96801)", "Seijun Suzuki (tmdb:82461)", "Susumu Hani (tmdb:1041699)", "Toshio Matsumoto (tmdb:17542)", "Koji Wakamatsu (tmdb:134811)", "Yasuzo Masumura (tmdb:81273)", "Shuji Terayama (tmdb:137495)", "Akio Jissoji (tmdb:138863)", "Kazuo Kuroki (tmdb:224653)"])
            .WithTranslation("fr", "Nouvelle Vague japonaise", "Politique radicale, sexe et violence contre le système des studios."),
        MovementDefinition.Create(
            "french-new-wave",
            "French New Wave",
            "Handheld cameras, jump cuts and the critics of Cahiers du cinéma behind the camera.",
            ["FR"],
            (1958, 1973),
            ["Jean-Luc Godard (tmdb:3776)", "François Truffaut (tmdb:1650)", "Claude Chabrol (tmdb:19069)", "Éric Rohmer (tmdb:28615)", "Jacques Rivette (tmdb:73153)", "Agnès Varda (tmdb:6817)", "Alain Resnais (tmdb:11983)", "Jacques Demy (tmdb:24882)", "Louis Malle (tmdb:15389)", "Chris Marker (tmdb:9956)", "Jacques Rozier (tmdb:237666)", "Jean Eustache (tmdb:53914)", "Jean Rouch (tmdb:238960)", "Jacques Doniol-Valcroze (tmdb:130034)", "Pierre Kast (tmdb:935110)", "Luc Moullet (tmdb:236634)", "Jean-Daniel Pollet (tmdb:237653)"])
            .WithTranslation("fr", "Nouvelle Vague", "Caméra à l'épaule, faux raccords et critiques des Cahiers du cinéma passés derrière la caméra."),
        MovementDefinition.Create(
            "british-new-wave",
            "British New Wave",
            "Kitchen-sink realism and angry young men in the industrial North.",
            ["GB"],
            (1958, 1968),
            ["Tony Richardson (tmdb:32765)", "Karel Reisz (tmdb:72709)", "Lindsay Anderson (tmdb:78157)", "John Schlesinger (tmdb:19304)", "Jack Clayton (tmdb:25630)"])
            .WithTranslation("fr", "Nouvelle Vague britannique", "Réalisme social et jeunes gens en colère dans le Nord industriel."),
        MovementDefinition.Create(
            "cinema-novo",
            "Cinema Novo",
            "\"A camera in hand and an idea in the head\": Brazil's political cinema.",
            ["BR"],
            (1960, 1972),
            ["Glauber Rocha (tmdb:544845)", "Nelson Pereira dos Santos (tmdb:12386)", "Ruy Guerra (tmdb:20548)", "Carlos Diegues (tmdb:112117)", "Joaquim Pedro de Andrade (tmdb:254884)", "Leon Hirszman (tmdb:1185869)", "Paulo César Saraceni (tmdb:1154787)", "Arnaldo Jabor (tmdb:531780)", "Gustavo Dahl (tmdb:1175689)", "Linduarte Noronha (tmdb:1437613)", "Walter Lima Jr. (tmdb:150495)"])
            .WithTranslation("fr", "Cinema Novo", "« Une caméra à la main et une idée dans la tête » : le cinéma politique brésilien."),
        MovementDefinition.Create(
            "spaghetti-western",
            "Spaghetti Western",
            "The Italian reinvention of the western, shot in Almería and scored by Morricone.",
            ["IT", "ES"],
            (1960, 1978),
            [],
            ["Western"])
            .WithTranslation("fr", "Western spaghetti", "La réinvention italienne du western, tournée à Almería sur des musiques de Morricone."),
        MovementDefinition.Create(
            "czechoslovak-new-wave",
            "Czechoslovak New Wave",
            "Absurd humour and quiet defiance before the Prague Spring was crushed.",
            ["XC", "CZ", "SK"],
            (1962, 1970),
            ["Miloš Forman (tmdb:3974)", "Věra Chytilová (tmdb:124137)", "Jiří Menzel (tmdb:11720)", "Jan Němec (tmdb:1024901)", "Ivan Passer (tmdb:84736)", "Juraj Herz (tmdb:83998)", "Ján Kadár (tmdb:544700)", "Elmar Klos (tmdb:544699)", "Juraj Jakubisko (tmdb:78001)", "František Vláčil (tmdb:226662)", "Pavel Juráček (tmdb:119999)", "Jaromil Jireš (tmdb:588758)", "Evald Schorm (tmdb:1035927)", "Vojtěch Jasný (tmdb:137071)", "Štefan Uher (tmdb:137036)", "Dušan Hanák (tmdb:135449)"])
            .WithTranslation("fr", "Nouvelle Vague tchécoslovaque", "Humour absurde et défiance tranquille, jusqu'à l'écrasement du Printemps de Prague."),
        MovementDefinition.Create(
            "new-german-cinema",
            "New German Cinema",
            "The Oberhausen generation confronting post-war Germany.",
            ["DE"],
            (1962, 1982),
            ["Rainer Werner Fassbinder (tmdb:2725)", "Werner Herzog (tmdb:6818)", "Wim Wenders (tmdb:2303)", "Volker Schlöndorff (tmdb:10249)", "Margarethe von Trotta (tmdb:39298)", "Alexander Kluge (tmdb:50870)", "Hans-Jürgen Syberberg (tmdb:227824)", "Werner Schroeter (tmdb:45192)", "Edgar Reitz (tmdb:72908)", "Reinhard Hauff (tmdb:234362)", "Helma Sanders-Brahms (tmdb:56203)", "Jean-Marie Straub (tmdb:935136)", "Danièle Huillet (tmdb:935137)", "Peter Fleischmann (tmdb:37313)", "Hark Bohm (tmdb:1865)"])
            .WithTranslation("fr", "Nouveau cinéma allemand", "La génération d'Oberhausen face à l'Allemagne d'après-guerre."),
        MovementDefinition.Create(
            "giallo",
            "Giallo",
            "Black gloves, baroque murders and lurid colour in the Italian thriller.",
            [],
            (1963, 1985),
            [])
            .WithTranslation("fr", "Giallo", "Gants noirs, meurtres baroques et couleurs vives dans le thriller italien."),
        MovementDefinition.Create(
            "yugoslav-black-wave",
            "Yugoslav Black Wave",
            "Dark satire that turned socialist optimism on its head.",
            ["YU"],
            (1963, 1973),
            ["Dušan Makavejev (tmdb:17554)", "Aleksandar Petrović (tmdb:74378)", "Živojin Pavlović (tmdb:1047280)", "Želimir Žilnik (tmdb:1051272)", "Puriša Đorđević (tmdb:1186494)"])
            .WithTranslation("fr", "Vague noire yougoslave", "Une satire sombre qui renverse l'optimisme socialiste."),
        MovementDefinition.Create(
            "new-hollywood",
            "New Hollywood",
            "Directors take over the studios, from Bonnie and Clyde to Heaven's Gate.",
            ["US"],
            (1967, 1980),
            ["Arthur Penn (tmdb:6448)", "Mike Nichols (tmdb:5342)", "Dennis Hopper (tmdb:2778)", "Robert Altman (tmdb:9789)", "Francis Ford Coppola (tmdb:1776)", "Martin Scorsese (tmdb:1032)", "Hal Ashby (tmdb:4964)", "Peter Bogdanovich (tmdb:39012)", "William Friedkin (tmdb:15175)", "Bob Rafelson (tmdb:19450)", "Terrence Malick (tmdb:30715)", "Brian De Palma (tmdb:1150)", "Michael Cimino (tmdb:12114)", "Monte Hellman (tmdb:20921)", "Alan J. Pakula (tmdb:6349)", "Sidney Lumet (tmdb:39996)", "Woody Allen (tmdb:1243)", "Paul Mazursky (tmdb:47773)", "Sam Peckinpah (tmdb:7767)", "John Schlesinger (tmdb:19304)", "George Roy Hill (tmdb:9577)", "Sydney Pollack (tmdb:2226)", "John Milius (tmdb:8328)", "Bob Fosse (tmdb:66777)", "John Boorman (tmdb:19665)", "Roman Polanski (tmdb:3556)", "Paul Schrader (tmdb:1035)"])
            .WithTranslation("fr", "Nouvel Hollywood", "Les réalisateurs prennent le pouvoir, de Bonnie and Clyde à La Porte du paradis."),
        MovementDefinition.Create(
            "iranian-new-wave",
            "Iranian New Wave",
            "Poetic realism blurring fiction and documentary.",
            ["IR"],
            (1969, 2010),
            ["Abbas Kiarostami (tmdb:119294)", "Mohsen Makhmalbaf (tmdb:120226)", "Jafar Panahi (tmdb:120229)", "Dariush Mehrjui (tmdb:120227)", "Majid Majidi (tmdb:110695)", "Bahman Ghobadi (tmdb:54607)", "Samira Makhmalbaf (tmdb:20026)", "Bahram Beyzaie (tmdb:1098592)", "Sohrab Shahid-Saless (tmdb:1198165)", "Masoud Kimiai (tmdb:1316244)", "Amir Naderi (tmdb:72196)", "Nasser Taghvai (tmdb:1179182)", "Rakhshan Banietemad (tmdb:1061523)", "Ali Hatami (tmdb:1255940)", "Parviz Kimiavi (tmdb:1299850)"])
            .WithTranslation("fr", "Nouvelle Vague iranienne", "Un réalisme poétique qui brouille la frontière entre fiction et documentaire."),
        MovementDefinition.Create(
            "australian-new-wave",
            "Australian New Wave",
            "A national cinema reborn, from Hanging Rock to the outback.",
            ["AU"],
            (1970, 1988),
            ["Peter Weir (tmdb:2690)", "George Miller (tmdb:20629)", "Gillian Armstrong (tmdb:58094)", "Fred Schepisi (tmdb:64061)", "Bruce Beresford (tmdb:5696)", "Phillip Noyce (tmdb:13015)"])
            .WithTranslation("fr", "Nouvelle Vague australienne", "Un cinéma national renaît, de Hanging Rock à l'outback."),
        MovementDefinition.Create(
            "hong-kong-new-wave",
            "Hong Kong New Wave",
            "Television-trained directors who remade Hong Kong cinema, and the second wave that followed.",
            ["HK"],
            (1978, 1997),
            ["Tsui Hark (tmdb:26760)", "Ann Hui (tmdb:123199)", "Patrick Tam (tmdb:71063)", "Allen Fong (tmdb:975054)", "Yim Ho (tmdb:146295)", "Alex Cheung (tmdb:931207)", "Clara Law (tmdb:96449)", "Stanley Kwan (tmdb:96861)", "Wong Kar-wai (tmdb:12453)", "Fruit Chan (tmdb:56865)", "Mabel Cheung (tmdb:103602)", "Leong Po-chih (tmdb:17247)", "Ronny Yu (tmdb:17268)", "Dennis Yu (tmdb:1129469)", "Peter Yung (tmdb:1402406)", "Lawrence Ah Mon (tmdb:123195)"])
            .WithTranslation("fr", "Nouvelle Vague hongkongaise", "Des réalisateurs venus de la télévision refont le cinéma de Hong Kong, puis la seconde vague prend le relais."),
        MovementDefinition.Create(
            "taiwan-new-cinema",
            "Taiwan New Cinema",
            "Long takes and everyday life in a changing Taiwan.",
            ["TW"],
            (1982, 2000),
            ["Hou Hsiao-hsien (tmdb:64992)", "Edward Yang (tmdb:143035)", "Tsai Ming-liang (tmdb:71174)", "Wan Jen (tmdb:1158867)", "Wang Tung (tmdb:1158889)", "Ko I-chen (tmdb:1092245)", "Chen Kun-hou (tmdb:1158874)", "Wu Nien-jen (tmdb:143036)", "Chang Yi (tmdb:1092244)", "Tao Te-chen (tmdb:1092246)", "Lin Cheng-sheng (tmdb:131288)"])
            .WithTranslation("fr", "Nouveau cinéma taïwanais", "Plans longs et vie quotidienne dans un Taïwan en pleine mutation."),
        MovementDefinition.Create(
            "fifth-generation",
            "China's Fifth Generation",
            "The first class out of the Beijing Film Academy after the Cultural Revolution.",
            ["CN"],
            (1983, 1999),
            ["Zhang Yimou (tmdb:607)", "Chen Kaige (tmdb:20640)", "Tian Zhuangzhuang (tmdb:583343)", "Wu Ziniu (tmdb:1206522)", "Zhang Junzhao (tmdb:1184597)", "Hu Mei (tmdb:112064)", "Huang Jianxin (tmdb:134026)"])
            .WithTranslation("fr", "Cinquième génération chinoise", "La première promotion de l'Académie du film de Pékin après la Révolution culturelle."),
        MovementDefinition.Create(
            "heroic-bloodshed",
            "Heroic Bloodshed",
            "Brotherhood, betrayal and balletic gunfights from Hong Kong's golden age.",
            ["HK"],
            (1986, 1998),
            ["John Woo (tmdb:11401)", "Ringo Lam (tmdb:26767)", "Kirk Wong (tmdb:58036)", "Johnnie To (tmdb:25236)", "Andrew Lau (tmdb:65994)", "Gordon Chan (tmdb:64901)", "Benny Chan (tmdb:70109)", "Patrick Yau (tmdb:125653)", "Taylor Wong (tmdb:940641)"],
            ["Action", "Crime", "Thriller"])
            .WithTranslation("fr", "Heroic bloodshed", "Fraternité, trahison et fusillades chorégraphiées de l'âge d'or hongkongais."),
        MovementDefinition.Create(
            "korean-new-wave",
            "Korean New Wave",
            "Genre, rage and auteur ambition once censorship ended.",
            ["KR"],
            (1996, 2012),
            ["Bong Joon-ho (tmdb:21684)", "Park Chan-wook (tmdb:10099)", "Lee Chang-dong (tmdb:96657)", "Kim Jee-woon (tmdb:69081)", "Hong Sang-soo (tmdb:150975)", "Kim Ki-duk (tmdb:1188)"])
            .WithTranslation("fr", "Nouvelle Vague coréenne", "Cinéma de genre, rage et ambition d'auteur après la fin de la censure."),
        MovementDefinition.Create(
            "dogme-95",
            "Dogme 95",
            "Handheld cameras, natural light and a vow of chastity against artifice.",
            [],
            (1998, 2005),
            [])
            .WithTranslation("fr", "Dogme 95", "Caméra à l'épaule, lumière naturelle et vœu de chasteté contre l'artifice."),
        MovementDefinition.Create(
            "romanian-new-wave",
            "Romanian New Wave",
            "Long takes and deadpan humour in post-communist Romania.",
            ["RO"],
            (2001, 2022),
            ["Cristi Puiu (tmdb:107533)", "Cristian Mungiu (tmdb:20657)", "Corneliu Porumboiu (tmdb:109293)", "Radu Muntean (tmdb:129564)", "Cristian Nemescu (tmdb:1037092)", "Radu Jude (tmdb:588822)", "Călin Peter Netzer (tmdb:1137536)"])
            .WithTranslation("fr", "Nouvelle Vague roumaine", "Plans-séquences et humour pince-sans-rire dans la Roumanie post-communiste."),
    ];
}
