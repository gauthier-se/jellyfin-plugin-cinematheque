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
  var tileImageUser = null;

  // Prefer a library tile over a special view such as Live TV, whose styling may differ.
  function findLibraryTile() {
    var selectors = ['.card[data-type="CollectionFolder"]', '.card[data-type="UserView"]'];
    for (var s = 0; s < selectors.length; s++) {
      var tiles = document.querySelectorAll(selectors[s]);
      for (var i = tiles.length - 1; i >= 0; i--) {
        if (!tiles[i].closest('#cinematheque-root') && tiles[i].parentElement) {
          return tiles[i];
        }
      }
    }

    return null;
  }

  function addHomeTile() {
    var template = findLibraryTile();
    if (!template || template.parentElement.querySelector('[' + TILE_ATTR + ']')) {
      return;
    }

    // Keep data-type, which themes style library tiles by. Drop what jellyfin-web and other
    // plugins use to find the item behind the card, and the collection type, which some themes
    // use to swap in their own stock picture.
    var tile = template.cloneNode(true);
    tile.setAttribute(TILE_ATTR, '');
    ['data-id', 'data-serverid', 'data-path', 'data-index', 'data-action', 'data-collectiontype'].forEach(function (name) {
      tile.removeAttribute(name);
    });
    tile.querySelectorAll('*').forEach(function (el) {
      el.classList.remove('itemAction');
      ['data-action', 'data-id', 'data-serverid', 'data-type', 'data-isfolder'].forEach(function (name) { el.removeAttribute(name); });
      if (el.tagName === 'A') {
        el.setAttribute('href', HREF);
      }
    });
    tile.querySelectorAll('.cardOverlayButton, .cardOverlayButton-br, .cardIndicators, .countIndicator, .blurhash-canvas, .cardImageIcon').forEach(function (el) {
      el.remove();
    });
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
      image.style.backgroundImage = '';
      setTileImage(image);
    }

    template.parentElement.appendChild(tile);
  }

  // A film backdrop under a veil in the logo's colours, with the name written over it the way the
  // server writes it on the pictures it generates for libraries. Drawn on a canvas, which keeps
  // other plugins from mistaking the tile for that film. Drawn again for each user: signing out
  // does not reload the page, and the next user may not have access to that film.
  var TILE_VEIL = ['rgba(110, 45, 140, 0.72)', 'rgba(0, 110, 160, 0.62)'];
  var TILE_WIDTH = 960;
  var TILE_HEIGHT = 540;

  function setTileImage(element) {
    var user = ApiClient.getCurrentUserId();
    if (tileImageUser !== user) {
      tileImage = null;
    }

    if (!tileImage) {
      tileImageUser = user;
      tileImage = ApiClient.getJSON(ApiClient.getUrl('Users/' + user + '/Items', {
        IncludeItemTypes: 'Movie',
        Recursive: true,
        ImageTypes: 'Backdrop',
        SortBy: 'Random',
        Limit: 1
      })).then(function (result) {
        var film = result.Items && result.Items[0];
        if (!film) {
          throw new Error('Cinematheque: no backdrop');
        }

        return ApiClient.fetch({ url: ApiClient.getScaledImageUrl(film.Id, { type: 'Backdrop', maxWidth: TILE_WIDTH, quality: 90 }), type: 'GET' });
      }).then(function (response) {
        return response.blob();
      }).then(loadPicture).catch(function () {
        return null;
      }).then(function (picture) {
        return drawTile(picture, label());
      });
    }

    element.style.backgroundImage = 'linear-gradient(135deg, ' + TILE_VEIL.join(', ') + ')';
    tileImage.then(function (url) {
      element.style.backgroundImage = 'url("' + url + '")';
    });
  }

  function loadPicture(blob) {
    return new Promise(function (resolve, reject) {
      var url = URL.createObjectURL(blob);
      var picture = new Image();
      picture.onload = function () {
        URL.revokeObjectURL(url);
        resolve(picture);
      };
      picture.onerror = function () {
        URL.revokeObjectURL(url);
        reject(new Error('Cinematheque: unreadable backdrop'));
      };
      picture.src = url;
    });
  }

  function drawTile(picture, name) {
    var canvas = document.createElement('canvas');
    canvas.width = TILE_WIDTH;
    canvas.height = TILE_HEIGHT;
    var context = canvas.getContext('2d');
    context.fillStyle = '#000';
    context.fillRect(0, 0, TILE_WIDTH, TILE_HEIGHT);

    if (picture) {
      var scale = Math.max(TILE_WIDTH / picture.width, TILE_HEIGHT / picture.height);
      var width = picture.width * scale;
      var height = picture.height * scale;
      context.drawImage(picture, (TILE_WIDTH - width) / 2, (TILE_HEIGHT - height) / 2, width, height);
    }

    // The gradient line of CSS's 135deg, so the veil looks as it did when it was a CSS gradient.
    var reach = (TILE_WIDTH + TILE_HEIGHT) / 4;
    var veil = context.createLinearGradient(TILE_WIDTH / 2 - reach, TILE_HEIGHT / 2 - reach, TILE_WIDTH / 2 + reach, TILE_HEIGHT / 2 + reach);
    veil.addColorStop(0, TILE_VEIL[0]);
    veil.addColorStop(1, TILE_VEIL[1]);
    context.fillStyle = veil;
    context.fillRect(0, 0, TILE_WIDTH, TILE_HEIGHT);

    // The server's recipe: bold, 112px on a 960px wide picture, shrunk to 90% of the width when it
    // would take more than 95%. In the web client's font, which is loaded by the time tiles render.
    var font = getComputedStyle(document.body).fontFamily || 'sans-serif';
    var size = 112;
    context.font = 'bold ' + size + 'px ' + font;
    var textWidth = context.measureText(name).width;
    if (textWidth > TILE_WIDTH * 0.95) {
      size = 0.9 * TILE_WIDTH * size / textWidth;
      context.font = 'bold ' + size + 'px ' + font;
    }

    context.fillStyle = '#fff';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(name, TILE_WIDTH / 2, TILE_HEIGHT / 2);
    return canvas.toDataURL('image/jpeg', 0.9);
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

  // MUI colours a button through classes it generates from its props, so swapping
  // MuiButton-colorInherit for MuiButton-colorPrimary does not turn the header entry blue by
  // itself. These rules do, as jellyfin-web colours the current library: in the theme's primary
  // colour, tinted on hover.
  var style = document.createElement('style');
  style.textContent =
    'header [' + ENTRY_ATTR + '].MuiButton-colorPrimary{color:var(--jf-palette-primary-main,#00a4dc)}' +
    'header [' + ENTRY_ATTR + '].MuiButton-colorPrimary:hover{background-color:' +
    'rgba(var(--jf-palette-primary-mainChannel,0 164 220)/var(--jf-palette-action-hoverOpacity,0.08))}';
  document.head.appendChild(style);

  // React and the legacy view manager both rebuild the header and pages on navigation.
  new MutationObserver(function (mutations) {
    var ours = mutations.every(function (m) {
      return m.target.closest && m.target.closest('#cinematheque-root, #cinematheque-bar, [' + ENTRY_ATTR + '], [' + TILE_ATTR + ']');
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
