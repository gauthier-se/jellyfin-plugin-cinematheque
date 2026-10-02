/*
 * Cinematheque loader, injected into jellyfin-web's index.html by File Transformation.
 *
 * It does two things and stays small: it adds a "Cinematheque" entry next to Favorites in
 * whichever layout is active, and it loads the real app the first time the user opens it.
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
      return m.target.closest && m.target.closest('#cinematheque-root, [' + ENTRY_ATTR + ']');
    });
    if (!ours) {
      schedule();
    }
  }).observe(document.body, { childList: true, subtree: true });

  window.addEventListener('hashchange', schedule);
  schedule();
})();
