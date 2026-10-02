/*
 * Cinematheque web app. Loaded by loader.js the first time the view is opened.
 *
 * State lives in the URL (#/home?cinematheque=<view>&...), so every screen can be bookmarked
 * and the back button works. The DOM is built with createElement only: names and titles come
 * from library metadata and are never parsed as HTML.
 */
(function () {
  'use strict';

  var ROOT_ID = 'cinematheque-root';
  var ACTIVE_CLASS = 'cinematheque-host';
  var PAGE_SIZE = 120;

  // ---------------------------------------------------------------- i18n

  var STRINGS = {
    en: {
      title: 'Cinematheque',
      directors: 'Directors',
      actors: 'Actors',
      writers: 'Writers',
      countries: 'Countries',
      movements: 'Movements',
      search: 'Search',
      sortCount: 'Most films',
      sortName: 'A to Z',
      minFilms: 'At least {0} films',
      minFilm: 'At least 1 film',
      films: '{0} films',
      film: '1 film',
      allDecades: 'All decades',
      loadMore: 'Show more',
      loading: 'Loading…',
      empty: 'Nothing here yet.',
      error: 'Cinematheque could not load this page.',
      openInJellyfin: 'Open in Jellyfin',
      topDirectors: 'Leading directors',
      noMovementFilms: 'None in your library',
      minimum: 'Minimum number of films',
      defaultMin: 'Default minimum',
      seen: '{0} seen',
      unseenOnly: 'Not seen yet',
      seenBadge: 'Seen',
      pick: 'Pick a film for me',
      coproductions: 'Include co-productions'
    },
    fr: {
      title: 'Cinémathèque',
      directors: 'Réalisateurs',
      actors: 'Acteurs',
      writers: 'Scénaristes',
      countries: 'Pays',
      movements: 'Mouvements',
      search: 'Rechercher',
      sortCount: 'Plus de films',
      sortName: 'De A à Z',
      minFilms: 'Au moins {0} films',
      minFilm: 'Au moins 1 film',
      films: '{0} films',
      film: '1 film',
      allDecades: 'Toutes les décennies',
      loadMore: 'Afficher plus',
      loading: 'Chargement…',
      empty: 'Rien pour le moment.',
      error: 'La Cinémathèque n\'a pas pu charger cette page.',
      openInJellyfin: 'Ouvrir dans Jellyfin',
      topDirectors: 'Principaux réalisateurs',
      noMovementFilms: 'Aucun dans votre bibliothèque',
      minimum: 'Nombre minimum de films',
      defaultMin: 'Minimum par défaut',
      seen: '{0} vus',
      unseenOnly: 'Pas encore vus',
      seenBadge: 'Vu',
      pick: 'Programme-moi un film',
      coproductions: 'Inclure les coproductions'
    }
  };

  // Codes TMDB uses for states that no longer exist; Intl cannot name them.
  var HISTORICAL = {
    en: { SU: 'Soviet Union', YU: 'Yugoslavia', XC: 'Czechoslovakia', XG: 'East Germany', CS: 'Serbia and Montenegro' },
    fr: { SU: 'Union soviétique', YU: 'Yougoslavie', XC: 'Tchécoslovaquie', XG: 'Allemagne de l\'Est', CS: 'Serbie-et-Monténégro' }
  };

  function language() {
    return (document.documentElement.lang || navigator.language || 'en').toLowerCase();
  }

  function t(key, arg) {
    var table = STRINGS[language().slice(0, 2)] || STRINGS.en;
    var text = table[key] || STRINGS.en[key] || key;
    return arg === undefined ? text : text.replace('{0}', arg);
  }

  function filmCount(n) {
    return n === 1 ? t('film') : t('films', n.toLocaleString(language()));
  }

  // A thin bar, hidden from assistive tech: the text next to it carries the numbers.
  function progress(seen, total) {
    var ratio = total ? Math.min(1, seen / total) : 0;
    return h('span', { class: 'cin-progress', 'aria-hidden': 'true' },
      h('span', { class: 'cin-progress-bar', style: 'width:' + Math.round(ratio * 100) + '%' }));
  }

  var regionNames = null;

  function countryName(country) {
    var historical = HISTORICAL[language().slice(0, 2)] || HISTORICAL.en;
    if (historical[country.Code]) {
      return historical[country.Code];
    }

    if (/^[A-Z]{2}$/.test(country.Code)) {
      try {
        regionNames = regionNames || new Intl.DisplayNames([language()], { type: 'region', fallback: 'none' });
        return regionNames.of(country.Code) || country.Name;
      } catch (e) {
        return country.Name;
      }
    }

    return country.Name;
  }

  function years(from, to) {
    if (!from) {
      return '';
    }

    return from === to ? String(from) : from + '–' + to;
  }

  // ---------------------------------------------------------------- DOM

  function h(tag, attrs) {
    var el = document.createElement(tag);
    if (attrs) {
      Object.keys(attrs).forEach(function (key) {
        var value = attrs[key];
        if (value === null || value === undefined || value === false) {
          return;
        }

        if (key === 'class') {
          el.className = value;
        } else if (key === 'text') {
          el.textContent = value;
        } else if (key.indexOf('on') === 0) {
          el.addEventListener(key.slice(2), value);
        } else {
          el.setAttribute(key, value === true ? '' : value);
        }
      });
    }

    for (var i = 2; i < arguments.length; i++) {
      append(el, arguments[i]);
    }

    return el;
  }

  function append(parent, child) {
    if (child === null || child === undefined || child === false) {
      return;
    }

    if (Array.isArray(child)) {
      child.forEach(function (c) { append(parent, c); });
    } else {
      parent.appendChild(typeof child === 'string' ? document.createTextNode(child) : child);
    }
  }

  // Route values come from the URL, so never let them reach Object.prototype.
  function own(object, key) {
    return Object.prototype.hasOwnProperty.call(object, key);
  }

  function initials(name) {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map(function (part) { return part[0]; }).join('').toUpperCase();
  }

  // The overlay reuses jellyfin-web's own card classes, so hovering a Cinematheque card looks
  // exactly like hovering any other card, theme included.
  function image(id, height, alt, fallbackText) {
    var frame = h('div', { class: 'cin-image' }, h('span', { class: 'cin-image-fallback', 'aria-hidden': 'true', text: fallbackText }));
    var img = h('img', {
      loading: 'lazy',
      alt: alt,
      src: ApiClient.getScaledImageUrl(id, { type: 'Primary', maxHeight: height, quality: 90 })
    });
    img.addEventListener('load', function () { frame.classList.add('cin-image-loaded'); });
    img.addEventListener('error', function () { img.remove(); });
    frame.appendChild(img);
    frame.appendChild(h('span', { class: 'cardOverlayContainer', 'aria-hidden': 'true' }));
    return frame;
  }

  // ---------------------------------------------------------------- routing

  // The parameters the views read. The URL is anyone's to write, so only these become route
  // properties: a parameter named __proto__ or constructor never reaches an object.
  var ROUTE_KEYS = ['person', 'name', 'country', 'movement', 'decade', 'unseen', 'primary', 'q', 'sort', 'min', 'limit'];

  function readRoute() {
    var params = new URLSearchParams(location.hash.split('?')[1] || '');
    var route = { view: params.get('cinematheque') || 'directors' };
    ROUTE_KEYS.forEach(function (key) {
      if (params.has(key)) {
        route[key] = params.get(key);
      }
    });
    return route;
  }

  function href(route) {
    var params = new URLSearchParams();
    params.set('cinematheque', route.view);
    Object.keys(route).forEach(function (key) {
      if (key !== 'view' && key !== 'cinematheque' && route[key] !== undefined && route[key] !== null && route[key] !== '') {
        params.set(key, route[key]);
      }
    });
    return '#/home?' + params.toString();
  }

  function navigate(route, replace) {
    var target = href(route);
    if (replace) {
      history.replaceState(history.state, '', target);
      render(true);
    } else {
      location.hash = target.slice(1);
    }
  }

  // Records a change that is already on screen, such as a longer list, so the back button
  // restores it, without rendering again.
  function remember(route) {
    history.replaceState(history.state, '', href(route));
    renderedKey = renderKey();
  }

  function itemHref(id) {
    return '#/details?id=' + encodeURIComponent(id) + '&serverId=' + encodeURIComponent(ApiClient.serverId());
  }

  // ---------------------------------------------------------------- API

  // Short-lived, so a long-open tab still picks up library changes. Signing out does not reload
  // the page and URLs do not name the user, so the cache is dropped when the user changes:
  // the next one must not see lists built from the previous one's libraries.
  var CACHE_MS = 5 * 60 * 1000;
  var cache = new Map();
  var cacheUser = null;

  function api(path, params) {
    var user = ApiClient.getCurrentUserId();
    if (user !== cacheUser) {
      cache.clear();
      cacheUser = user;
    }

    var url = ApiClient.getUrl('Cinematheque/' + path, params || {});
    var hit = cache.get(url);
    if (hit && Date.now() - hit.time < CACHE_MS) {
      return hit.promise;
    }

    var promise = ApiClient.getJSON(url).catch(function (err) {
      cache.delete(url);
      throw err;
    });
    cache.set(url, { time: Date.now(), promise: promise });
    return promise;
  }

  // ---------------------------------------------------------------- host

  // Modern layout renders pages into <main>; legacy layouts into the visible home page.
  function findHost() {
    return document.querySelector('main') || document.querySelector('#indexPage:not(.hide)') || document.querySelector('.page:not(.hide)');
  }

  var renderedKey = null;
  var renderToken = 0;

  // The user is part of the key, so the view is rebuilt for whoever signs in next.
  function renderKey() {
    return ApiClient.getCurrentUserId() + location.hash;
  }

  function render(force) {
    var host = findHost();
    if (!host) {
      return;
    }

    var root = document.getElementById(ROOT_ID);
    if (root && root.parentElement !== host) {
      root.remove();
      root = null;
    }

    var key = renderKey();
    if (root && key === renderedKey && !force) {
      return;
    }

    if (!root) {
      root = h('div', { id: ROOT_ID, class: 'cinematheque' });
      host.appendChild(root);
    }

    host.classList.add(ACTIVE_CLASS);
    renderedKey = key;
    var token = ++renderToken;
    var route = readRoute();

    root.replaceChildren(header(route));
    var body = h('div', { class: 'cin-body' }, h('p', { class: 'cin-status', text: t('loading') }));
    root.appendChild(body);

    var view = VIEWS.has(route.view) ? VIEWS.get(route.view) : VIEWS.get('directors');
    Promise.resolve(view(route)).then(function (content) {
      if (token === renderToken) {
        body.replaceChildren(content);
      }
    }).catch(function (err) {
      console.error('Cinematheque:', err);
      if (token === renderToken) {
        body.replaceChildren(h('p', { class: 'cin-status cin-error', text: t('error') }));
      }
    });
  }

  function unmount() {
    var root = document.getElementById(ROOT_ID);
    if (root) {
      root.remove();
    }

    document.querySelectorAll('.' + ACTIVE_CLASS).forEach(function (el) { el.classList.remove(ACTIVE_CLASS); });
    renderedKey = null;
  }

  // ---------------------------------------------------------------- shared pieces

  var SECTIONS = ['directors', 'actors', 'writers', 'countries', 'movements'];
  var SECTION_OF = {
    directors: 'directors', director: 'directors',
    actors: 'actors', actor: 'actors',
    writers: 'writers', writer: 'writers',
    countries: 'countries', country: 'countries',
    movements: 'movements', movement: 'movements'
  };

  function header(route) {
    var current = own(SECTION_OF, route.view) ? SECTION_OF[route.view] : 'directors';
    return h('header', { class: 'cin-header' },
      h('h1', { class: 'cin-title', text: t('title') }),
      h('nav', { class: 'cin-tabs', 'aria-label': t('title') }, SECTIONS.map(function (section) {
        return h('a', {
          class: 'cin-tab' + (section === current ? ' cin-tab-active' : ''),
          href: href({ view: section }),
          'aria-current': section === current ? 'page' : null,
          text: t(section)
        });
      })));
  }

  function empty() {
    return h('p', { class: 'cin-status', text: t('empty') });
  }

  function filmCard(film) {
    return h('li', null, h('a', { class: 'cin-card card-hoverable', href: itemHref(film.Id) },
      image(film.Id, 360, '', initials(film.Name)),
      film.Seen ? h('span', { class: 'cin-seen-badge material-icons', role: 'img', 'aria-label': t('seenBadge'), title: t('seenBadge'), text: 'check' }) : null,
      h('span', { class: 'cin-card-title', text: film.Name }),
      h('span', { class: 'cin-card-meta', text: [film.Year, film.Directors.slice(0, 2).join(', ')].filter(Boolean).join(' · ') })));
  }

  function filmGrid(films) {
    return films.length ? h('ul', { class: 'cin-grid cin-grid-posters' }, films.map(filmCard)) : empty();
  }

  // ---------------------------------------------------------------- paging

  // The server caps a page at 500 items, so lists grow one page of PAGE_SIZE at a time.
  // fetchPage(startIndex) returns that page; pages share their URLs, and so the cache.

  // Loads the first `count` items of a list, one page after another.
  function fetchList(fetchPage, count) {
    return fetchPage(0).then(function (first) {
      var end = Math.min(count, first.TotalRecordCount);
      var requests = [];
      for (var start = PAGE_SIZE; start < end; start += PAGE_SIZE) {
        requests.push(fetchPage(start));
      }

      return Promise.all(requests).then(function (pages) {
        var items = first.Items;
        pages.forEach(function (page) { items = items.concat(page.Items); });
        return Object.assign({}, first, { Items: items });
      });
    });
  }

  // "Show more": appends the next page to the list in place, then calls onGrow with the number
  // of items now shown. Nothing when the list is complete.
  function moreButton(list, page, fetchPage, toItem, onGrow) {
    var loaded = page.Items.length;
    if (loaded >= page.TotalRecordCount) {
      return null;
    }

    var button = h('button', { class: 'cin-more', type: 'button', text: t('loadMore') });
    button.addEventListener('click', function () {
      button.disabled = true;
      fetchPage(loaded).then(function (next) {
        append(list, next.Items.map(toItem));
        loaded += next.Items.length;
        if (onGrow) {
          onGrow(loaded);
        }

        if (!next.Items.length || loaded >= next.TotalRecordCount) {
          button.remove();
        } else {
          button.disabled = false;
        }
      }).catch(function (err) {
        console.error('Cinematheque:', err);
        button.disabled = false;
      });
    });
    return button;
  }

  function decadeChips(route, decades) {
    if (decades.length < 2) {
      return null;
    }

    var chips = [h('a', {
      class: 'cin-chip' + (!route.decade ? ' cin-chip-active' : ''),
      href: href(Object.assign({}, route, { decade: null })),
      text: t('allDecades')
    })];
    decades.forEach(function (d) {
      chips.push(h('a', {
        class: 'cin-chip' + (String(d.Decade) === route.decade ? ' cin-chip-active' : ''),
        href: href(Object.assign({}, route, { decade: d.Decade })),
        title: filmCount(d.FilmCount)
      }, d.Decade + 's', h('span', { class: 'cin-chip-count', text: String(d.FilmCount) })));
    });
    return h('div', { class: 'cin-chips', role: 'group' }, chips);
  }

  // Film list with decade filter and "show more", for any filter combination.
  function filmsSection(route, filter) {
    var params = Object.assign({}, filter);
    if (route.decade) {
      params.decade = route.decade;
    }

    if (route.unseen) {
      params.unseen = true;
    }

    function fetchPage(startIndex) {
      return api('Films', Object.assign({ startIndex: startIndex, limit: PAGE_SIZE }, params));
    }

    return fetchPage(0).then(function (page) {
      var total = route.unseen ? page.TotalRecordCount + page.SeenCount : page.TotalRecordCount;
      var grid = filmGrid(page.Items);
      return h('section', { class: 'cin-films' },
        decadeChips(route, page.Decades),
        h('div', { class: 'cin-count' },
          h('span', { text: filmCount(total) + ' · ' + t('seen', page.SeenCount) }),
          progress(page.SeenCount, total),
          h('a', {
            class: 'cin-chip cin-chip-small' + (route.unseen ? ' cin-chip-active' : ''),
            href: href(Object.assign({}, route, { unseen: route.unseen ? null : 1 })),
            'aria-pressed': route.unseen ? 'true' : 'false',
            text: t('unseenOnly')
          }),
          total ? h('button', {
            class: 'cin-chip cin-chip-small cin-pick',
            type: 'button',
            onclick: function () { pickFilm(filter, route.decade); }
          }, h('span', { class: 'material-icons', 'aria-hidden': 'true', text: 'shuffle' }), t('pick')) : null),
        grid,
        moreButton(grid, page, fetchPage, filmCard));
    });
  }

  // Never cached: each click must draw again.
  function pickFilm(filter, decade) {
    var params = Object.assign({}, filter);
    if (decade) {
      params.decade = decade;
    }

    ApiClient.getJSON(ApiClient.getUrl('Cinematheque/Films/Random', params)).then(function (film) {
      location.hash = itemHref(film.Id).slice(1);
    }).catch(function (err) { console.error('Cinematheque:', err); });
  }

  // ---------------------------------------------------------------- views

  function peopleView(kind) {
    return function (route) {
      var endpoint = 'People/' + kind + 's';
      var params = { sortBy: route.sort || 'count' };
      if (route.q) {
        params.search = route.q;
      }

      if (route.min) {
        params.minFilms = route.min;
      }

      function fetchPage(startIndex) {
        return api(endpoint, Object.assign({ startIndex: startIndex, limit: PAGE_SIZE }, params));
      }

      // Name, then one line of what matters most; years and countries live on their page.
      function personCard(person) {
        var meta = filmCount(person.FilmCount) + (person.SeenCount ? ' · ' + t('seen', person.SeenCount) : '');
        return h('li', null, h('a', { class: 'cin-card cin-person card-hoverable', href: href({ view: kind, person: person.Key, name: person.Name }) },
          image(person.Id, 300, '', initials(person.Name)),
          h('span', { class: 'cin-card-title', text: person.Name }),
          h('span', { class: 'cin-card-meta', text: meta })));
      }

      // The URL keeps how far the list was opened, so coming back from a person restores it.
      return fetchList(fetchPage, Number(route.limit) || PAGE_SIZE).then(function (page) {
        var search = h('input', {
          class: 'cin-input',
          type: 'search',
          placeholder: t('search'),
          'aria-label': t('search'),
          value: route.q || ''
        });
        var debounce;
        search.addEventListener('input', function () {
          clearTimeout(debounce);
          debounce = setTimeout(function () {
            navigate(Object.assign({}, route, { q: search.value, limit: null }), true);
            var again = document.querySelector('#' + ROOT_ID + ' .cin-input');
            if (again) {
              again.focus();
              again.setSelectionRange(again.value.length, again.value.length);
            }
          }, 300);
        });

        var sort = h('select', {
          class: 'cin-input',
          'aria-label': t('sortCount'),
          onchange: function (e) { navigate(Object.assign({}, route, { sort: e.target.value, limit: null })); }
        },
        h('option', { value: 'count', text: t('sortCount'), selected: route.sort !== 'name' }),
        h('option', { value: 'name', text: t('sortName'), selected: route.sort === 'name' }));

        var currentMin = route.min || '';
        var min = h('select', {
          class: 'cin-input',
          'aria-label': t('minimum'),
          onchange: function (e) { navigate(Object.assign({}, route, { min: e.target.value, limit: null })); }
        },
        h('option', { value: '', text: t('defaultMin'), selected: currentMin === '' }),
        [1, 2, 3, 5, 10].map(function (n) {
          return h('option', { value: String(n), text: n === 1 ? t('minFilm') : t('minFilms', n), selected: currentMin === String(n) });
        }));

        var list = page.Items.length
          ? h('ul', { class: 'cin-grid cin-grid-people' }, page.Items.map(personCard))
          : empty();

        var more = moreButton(list, page, fetchPage, personCard, function (shown) {
          remember(Object.assign({}, route, { limit: shown }));
        });

        return h('div', null,
          h('div', { class: 'cin-toolbar' }, search, sort, min),
          list,
          more);
      });
    };
  }

  function personView(kind) {
    return function (route) {
      // Links carry the identity (tmdb:… or name:…); older links only have the name.
      var key = route.person || route.name;
      var filter = { person: key, role: kind + 's' };
      var details = api('People/' + kind + 's/' + encodeURIComponent(key)).catch(function () { return null; });
      return Promise.all([details, filmsSection(route, filter)]).then(function (results) {
        var person = results[0];
        var meta = person ? [years(person.FirstYear, person.LastYear), person.Countries.map(countryName).join(', ')].filter(Boolean).join(' · ') : '';
        return h('div', null,
          h('div', { class: 'cin-hero' },
            h('a', { class: 'cin-back', href: href({ view: kind + 's' }), text: '← ' + t(kind + 's') }),
            h('h2', { class: 'cin-hero-title', text: person ? person.Name : route.name }),
            meta ? h('p', { class: 'cin-movement-period', text: meta }) : null,
            h('a', { class: 'cin-link', href: '#', onclick: function (e) { e.preventDefault(); openPerson(person ? person.Name : route.name); }, text: t('openInJellyfin') })),
          results[1]);
      });
    };
  }

  function openPerson(name) {
    ApiClient.getJSON(ApiClient.getUrl('Persons/' + encodeURIComponent(name), { userId: ApiClient.getCurrentUserId() })).then(function (person) {
      location.hash = itemHref(person.Id).slice(1);
    }).catch(function (err) { console.error('Cinematheque:', err); });
  }

  // Co-productions count under every country by default; "primary" keeps the first one only.
  function countryParams(route) {
    return route.primary ? { primaryOnly: true } : {};
  }

  function coproductionToggle(route) {
    return h('a', {
      class: 'cin-chip cin-chip-small' + (route.primary ? '' : ' cin-chip-active'),
      href: href(Object.assign({}, route, { primary: route.primary ? null : 1 })),
      'aria-pressed': route.primary ? 'false' : 'true',
      text: t('coproductions')
    });
  }

  function countriesView(route) {
    return api('Countries', countryParams(route)).then(function (countries) {
      if (!countries.length) {
        return empty();
      }

      var max = countries.reduce(function (m, c) {
        return Math.max(m, c.Decades.reduce(function (n, d) { return Math.max(n, d.FilmCount); }, 0));
      }, 1);
      var minDecade = countries.reduce(function (m, c) { return c.Decades.length ? Math.min(m, c.Decades[0].Decade) : m; }, 3000);
      var maxDecade = countries.reduce(function (m, c) { return c.Decades.length ? Math.max(m, c.Decades[c.Decades.length - 1].Decade) : m; }, 0);

      return h('div', null, h('div', { class: 'cin-toolbar' }, coproductionToggle(route)), h('ul', { class: 'cin-grid cin-grid-countries' }, countries.map(function (country) {
        return h('li', null, h('a', { class: 'cin-country', href: href({ view: 'country', country: country.Code, primary: route.primary }) },
          h('span', { class: 'cin-country-name', text: countryName(country) }),
          h('span', { class: 'cin-card-meta', text: filmCount(country.FilmCount) + ' · ' + t('seen', country.SeenCount) }),
          progress(country.SeenCount, country.FilmCount),
          sparkline(country.Decades, minDecade, maxDecade, max),
          h('span', { class: 'cin-card-meta cin-card-countries', text: country.Directors.slice(0, 3).map(function (d) { return d.Name; }).join(', ') })));
      })));
    });
  }

  // One bar per decade on a shared scale, so countries can be compared at a glance.
  function sparkline(decades, minDecade, maxDecade, max) {
    var counts = {};
    decades.forEach(function (d) { counts[d.Decade] = d.FilmCount; });
    var bars = [];
    for (var decade = minDecade; decade <= maxDecade; decade += 10) {
      var n = counts[decade] || 0;
      bars.push(h('span', {
        class: 'cin-bar' + (n ? '' : ' cin-bar-empty'),
        style: 'height:' + (n ? Math.max(8, Math.round(Math.sqrt(n / max) * 100)) : 4) + '%',
        title: decade + 's: ' + n
      }));
    }

    return h('span', { class: 'cin-spark', 'aria-hidden': 'true' }, bars);
  }

  function countryView(route) {
    var filter = route.primary ? { country: route.country, primaryCountry: true } : { country: route.country };
    return Promise.all([api('Countries', countryParams(route)), filmsSection(route, filter)]).then(function (results) {
      var summary = results[0].find(function (c) { return c.Code === route.country; }) || { Code: route.country, Name: route.country, Directors: [] };
      return h('div', null,
        h('div', { class: 'cin-hero' },
          h('a', { class: 'cin-back', href: href({ view: 'countries', primary: route.primary }), text: '← ' + t('countries') }),
          h('h2', { class: 'cin-hero-title', text: countryName(summary) }),
          coproductionToggle(route)),
        summary.Directors.length ? h('div', { class: 'cin-related' },
          h('h3', { class: 'cin-subtitle', text: t('topDirectors') }),
          h('div', { class: 'cin-chips' }, summary.Directors.map(function (director) {
            return h('a', { class: 'cin-chip', href: href({ view: 'director', person: director.Key, name: director.Name }), text: director.Name });
          }))) : null,
        results[1]);
    });
  }

  function movementsView() {
    return api('Movements', { language: language() }).then(function (movements) {
      if (!movements.length) {
        return empty();
      }

      return h('ul', { class: 'cin-grid cin-grid-movements' }, movements.map(function (movement) {
        return h('li', null, h('a', {
          class: 'cin-movement' + (movement.FilmCount ? '' : ' cin-movement-empty'),
          href: href({ view: 'movement', movement: movement.Id })
        },
        h('span', { class: 'cin-movement-period', text: [years(movement.YearFrom, movement.YearTo), movement.Countries.map(countryName).join(', ')].filter(Boolean).join(' · ') }),
        h('span', { class: 'cin-country-name', text: movement.Name }),
        h('span', { class: 'cin-movement-description', text: movement.Description }),
        h('span', { class: 'cin-card-meta', text: movement.FilmCount ? filmCount(movement.FilmCount) + ' · ' + t('seen', movement.SeenCount) : t('noMovementFilms') }),
        movement.FilmCount ? progress(movement.SeenCount, movement.FilmCount) : null));
      }));
    });
  }

  function movementView(route) {
    return Promise.all([api('Movements', { language: language() }), filmsSection(route, { movement: route.movement })]).then(function (results) {
      var movement = results[0].find(function (m) { return m.Id === route.movement; }) || { Name: route.movement, Description: '', Countries: [] };
      return h('div', null,
        h('div', { class: 'cin-hero' },
          h('a', { class: 'cin-back', href: href({ view: 'movements' }), text: '← ' + t('movements') }),
          h('h2', { class: 'cin-hero-title', text: movement.Name }),
          h('p', { class: 'cin-movement-period', text: [years(movement.YearFrom, movement.YearTo), movement.Countries.map(countryName).join(', ')].filter(Boolean).join(' · ') }),
          movement.Description ? h('p', { class: 'cin-hero-text', text: movement.Description }) : null),
        results[1]);
    });
  }

  // A Map, so a view name read from the URL can only ever select one of these.
  var VIEWS = new Map([
    ['directors', peopleView('director')],
    ['actors', peopleView('actor')],
    ['writers', peopleView('writer')],
    ['director', personView('director')],
    ['actor', personView('actor')],
    ['writer', personView('writer')],
    ['countries', countriesView],
    ['country', countryView],
    ['movements', movementsView],
    ['movement', movementView]
  ]);

  window.Cinematheque = { render: function () { render(false); }, unmount: unmount };
})();
