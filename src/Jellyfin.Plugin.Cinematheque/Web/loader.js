/*
 * Cinematheque loader, injected into jellyfin-web's index.html by File Transformation.
 *
 * It stays small: it adds a "Cinematheque" entry next to Favorites in whichever layout is
 * active and a tile to "My Media" on the home page, and it loads the real app the first time the
 * user opens it.
 * The view lives on the home route (#/home?cinematheque=...), so the header, drawer and
 * back button keep working without registering a route in jellyfin-web.
 */
(function () {
  'use strict';

  if (window.CinemathequeLoader) {
    return;
  }

  var script = document.currentScript;
  var version = script ? new URL(script.src, location.href).searchParams.get('v') || '' : '';
  var base = script ? new URL('.', script.src).href : '../Cinematheque/Web/';
  var ENTRY_ATTR = 'data-cinematheque-entry';
  var LABELS = { fr: 'Cinémathèque' };

  function label() {
    var lang = (document.documentElement.lang || navigator.language || 'en').slice(0, 2).toLowerCase();
    return LABELS[lang] || 'Cinematheque';
  }

  function isActive() {
    return /^#\/home(\.html)?\?(.*&)?cinematheque=/.test(location.hash);
  }

  var HREF = '#/home?cinematheque=directors';

  // Jellyfin 12 modern layout: header buttons and, on small screens, the drawer list.
  // Both render a Favorites link to /home?tab=1, which is the most stable anchor available.
  function addModernEntries() {
    document.querySelectorAll('header a[href$="/home?tab=1"]').forEach(function (favorites) {
      var parent = favorites.parentElement;
      if (!parent || parent.querySelector('[' + ENTRY_ATTR + ']')) {
        return;
      }

      var entry = favorites.cloneNode(true);
      entry.setAttribute(ENTRY_ATTR, '');
      entry.setAttribute('href', HREF);
      entry.removeAttribute('aria-current');
      entry.classList.remove('Mui-selected');
      var icon = entry.querySelector('.MuiButton-startIcon');
      if (icon) {
        icon.innerHTML = '<span class="material-icons" aria-hidden="true" style="font-size:20px">movie_filter</span>';
      }

      setText(entry, label());
      favorites.after(entry);
    });

    document.querySelectorAll('.MuiDrawer-paper a[href$="/home?tab=1"]').forEach(function (favorites) {
      var item = favorites.closest('li');
      if (!item || !item.parentElement || item.parentElement.querySelector('[' + ENTRY_ATTR + ']')) {
        return;
      }

      var entry = item.cloneNode(true);
      entry.setAttribute(ENTRY_ATTR, '');
      var link = entry.querySelector('a');
      link.setAttribute('href', HREF);
      link.classList.remove('Mui-selected');
      var icon = entry.querySelector('.MuiListItemIcon-root');
      if (icon) {
        icon.innerHTML = '<span class="material-icons" aria-hidden="true">movie_filter</span>';
      }

      var text = entry.querySelector('.MuiListItemText-primary') || entry.querySelector('.MuiTypography-root');
      if (text) {
        text.textContent = label();
      }

      item.after(entry);
    });
  }

  // Legacy layouts (desktop-legacy, mobile-legacy, TV): the navigation drawer.
  function addLegacyEntries() {
    var home = document.querySelector('.mainDrawer a.navMenuOption[href="#/home"]');
    if (!home || home.parentElement.querySelector('[' + ENTRY_ATTR + ']')) {
      return;
    }

    var entry = home.cloneNode(true);
    entry.setAttribute(ENTRY_ATTR, '');
    entry.setAttribute('href', HREF);
    entry.classList.remove('navMenuOption-selected');
    var icon = entry.querySelector('.navMenuOptionIcon');
    if (icon) {
      icon.className = 'material-icons navMenuOptionIcon movie_filter';
    }

    var text = entry.querySelector('.navMenuOptionText');
    if (text) {
      text.textContent = label();
    }

    home.after(entry);
  }

  // "My Media" on the home page: library tiles. Home Screen Sections and themes render them their
  // own way, so the tile is a clone of whatever library tile is there, with its link, title and
  // image swapped. Only the web client gets it; TV and mobile apps reach movements through
  // collections instead.
  var TILE_ATTR = 'data-cinematheque-tile';
  var tileImage = null;

  function findLibraryTile() {
    var tiles = document.querySelectorAll('.card[data-type="CollectionFolder"], .card[data-type="UserView"]');
    for (var i = tiles.length - 1; i >= 0; i--) {
      if (!tiles[i].closest('#cinematheque-root') && tiles[i].parentElement) {
        return tiles[i];
      }
    }

    return null;
  }

  function addHomeTile() {
    var template = findLibraryTile();
    if (!template || template.parentElement.querySelector('[' + TILE_ATTR + ']')) {
      return;
    }

    var tile = template.cloneNode(true);
    tile.setAttribute(TILE_ATTR, '');
    ['data-id', 'data-type', 'data-collectiontype', 'data-action', 'data-isfolder', 'data-index'].forEach(function (name) {
      tile.removeAttribute(name);
    });

    // Strip jellyfin-web's own click handling, which would open the cloned library.
    tile.querySelectorAll('*').forEach(function (el) {
      el.classList.remove('itemAction');
      ['data-action', 'data-id', 'data-type', 'data-isfolder'].forEach(function (name) { el.removeAttribute(name); });
      if (el.tagName === 'A') {
        el.setAttribute('href', HREF);
      }
    });
    tile.querySelectorAll('.cardOverlayButton, .cardOverlayButton-br, .cardIndicators, .countIndicator').forEach(function (el) { el.remove(); });
    tile.addEventListener('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      location.hash = HREF.slice(1);
    }, true);

    var text = tile.querySelector('.cardText bdi') || tile.querySelector('.cardText');
    if (text) {
      text.textContent = label();
    }

    var image = tile.querySelector('.cardImageContainer');
    if (image) {
      image.classList.remove('lazy');
      image.removeAttribute('data-src');
      image.setAttribute('aria-label', label());
      image.querySelectorAll('.cardImageIcon, canvas').forEach(function (el) { el.remove(); });
      image.style.backgroundImage = '';
      setTileImage(image);
    }

    template.parentElement.appendChild(tile);
  }

  // One backdrop per page load: re-renders keep the same picture.
  function setTileImage(element) {
    if (!tileImage) {
      tileImage = ApiClient.getJSON(ApiClient.getUrl('Users/' + ApiClient.getCurrentUserId() + '/Items', {
        IncludeItemTypes: 'Movie',
        Recursive: true,
        ImageTypes: 'Backdrop',
        SortBy: 'Random',
        Limit: 1
      })).then(function (result) {
        var film = result.Items && result.Items[0];
        return film ? ApiClient.getScaledImageUrl(film.Id, { type: 'Backdrop', maxWidth: 800, quality: 90 }) : null;
      }).catch(function () { return null; });
    }

    tileImage.then(function (url) {
      if (url) {
        element.style.backgroundImage = 'url("' + url + '")';
      }
    });
  }

  // The header button keeps its label in a trailing text node after the icon span.
  function setText(element, text) {
    var node = Array.prototype.slice.call(element.childNodes).reverse().find(function (n) {
      return n.nodeType === Node.TEXT_NODE && n.textContent.trim() !== '';
    });
    if (node) {
      node.textContent = text;
    } else {
      element.appendChild(document.createTextNode(text));
    }
  }

  function markSelected() {
    var active = isActive();
    document.querySelectorAll('[' + ENTRY_ATTR + ']').forEach(function (entry) {
      var link = entry.matches('a') ? entry : entry.querySelector('a');
      if (!link) {
        return;
      }

      link.classList.toggle('Mui-selected', active && !!link.closest('.MuiDrawer-paper'));
      link.classList.toggle('navMenuOption-selected', active && link.classList.contains('navMenuOption'));
      if (link.closest('header')) {
        link.classList.toggle('MuiButton-colorPrimary', active);
        link.classList.toggle('MuiButton-colorInherit', !active);
      }
    });

    // Favorites must not look selected while we borrow the home route.
    if (active) {
      document.querySelectorAll('header a[href$="/home?tab=1"]').forEach(function (favorites) {
        favorites.classList.remove('MuiButton-colorPrimary');
        favorites.classList.add('MuiButton-colorInherit');
      });
    }
  }

  var appPromise = null;

  function loadApp() {
    if (!appPromise) {
      appPromise = new Promise(function (resolve, reject) {
        var css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = base + 'app.css?v=' + version;
        document.head.appendChild(css);

        var js = document.createElement('script');
        js.src = base + 'app.js?v=' + version;
        js.onload = function () { resolve(window.Cinematheque); };
        js.onerror = function () {
          appPromise = null;
          reject(new Error('Cinematheque: failed to load app.js'));
        };
        document.head.appendChild(js);
      });
    }

    return appPromise;
  }

  var pending = false;

  function sync() {
    pending = false;
    addModernEntries();
    addLegacyEntries();
    if (typeof ApiClient !== 'undefined' && ApiClient.getCurrentUserId()) {
      addHomeTile();
    }

    markSelected();

    if (isActive()) {
      loadApp().then(function (app) { app.render(); }).catch(function (err) { console.error(err); });
    } else if (window.Cinematheque) {
      window.Cinematheque.unmount();
    }
  }

  function schedule() {
    if (!pending) {
      pending = true;
      requestAnimationFrame(sync);
    }
  }

  window.CinemathequeLoader = { version: version };

  // React and the legacy view manager both rebuild the header and pages on navigation.
  new MutationObserver(function (mutations) {
    var ours = mutations.every(function (m) {
      return m.target.closest && m.target.closest('#cinematheque-root, [' + ENTRY_ATTR + '], [' + TILE_ATTR + ']');
    });
    if (!ours) {
      schedule();
    }
  }).observe(document.body, { childList: true, subtree: true });

  // Legacy layouts switch the home tabs (Home, Favorites) in place without touching the URL,
  // so the view would stay on top of them. Leave it by navigating to the chosen tab.
  document.addEventListener('click', function (e) {
    var tab = isActive() && e.target.closest ? e.target.closest('.emby-tabs-slider .emby-tab-button') : null;
    if (tab) {
      var index = tab.getAttribute('data-index');
      location.hash = index && index !== '0' ? '#/home?tab=' + index : '#/home';
    }
  }, true);

  window.addEventListener('hashchange', schedule);
  schedule();
})();
