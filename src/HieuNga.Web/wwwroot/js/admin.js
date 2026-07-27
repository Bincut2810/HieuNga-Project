/**
 * Admin shell — sidebar drawer (mobile) + collapse (desktop).
 * No business logic; safe for all Admin pages.
 */
(function () {
  'use strict';

  var COLLAPSE_KEY = 'hn-admin-sidebar-collapsed';

  function initSidebar() {
    var toggle = document.getElementById('admin-menu-toggle');
    var sidebar = document.getElementById('admin-sidebar');
    var overlay = document.getElementById('admin-sidebar-overlay');
    var collapseBtn = document.getElementById('admin-sidebar-collapse');
    var shell = document.querySelector('[data-admin-shell]');
    if (!toggle || !sidebar || !overlay) return;

    function setOpen(open) {
      sidebar.classList.toggle('is-open', open);
      overlay.classList.toggle('is-open', open);
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
      toggle.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
    }

    function setCollapsed(collapsed) {
      if (!shell) return;
      shell.classList.toggle('is-sidebar-collapsed', collapsed);
      sidebar.classList.toggle('is-collapsed', collapsed);
      try { localStorage.setItem(COLLAPSE_KEY, collapsed ? '1' : '0'); } catch (e) { /* ignore */ }
      if (collapseBtn) {
        collapseBtn.setAttribute('aria-label', collapsed ? 'Mở rộng menu' : 'Thu gọn menu');
        collapseBtn.setAttribute('title', collapsed ? 'Mở rộng' : 'Thu gọn');
      }
    }

    try {
      if (localStorage.getItem(COLLAPSE_KEY) === '1') setCollapsed(true);
    } catch (e) { /* ignore */ }

    toggle.addEventListener('click', function () {
      setOpen(!sidebar.classList.contains('is-open'));
    });
    overlay.addEventListener('click', function () {
      setOpen(false);
    });
    sidebar.querySelectorAll('a').forEach(function (link) {
      link.addEventListener('click', function () {
        if (window.innerWidth < 1024) setOpen(false);
      });
    });
    if (collapseBtn) {
      collapseBtn.addEventListener('click', function () {
        setCollapsed(!shell.classList.contains('is-sidebar-collapsed'));
      });
    }
  }

  document.addEventListener('DOMContentLoaded', function () {
    initSidebar();
  });
})();
