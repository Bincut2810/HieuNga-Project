/**
 * Test Ride admin board — tabs, confirms, tap-to-call, 30s visibility-aware polling.
 */
(function () {
  'use strict';

  var POLL_MS = 30000;

  function boot() {
    var root = document.querySelector('[data-ride-board]');
    if (!root || root.dataset.ready === '1') return;
    root.dataset.ready = '1';

    var refreshHost = root.querySelector('[data-ride-refresh]');
    var rangeInput = root.querySelector('[data-ride-range-input]');
    var qInput = root.querySelector('[data-ride-q]');
    var range = root.getAttribute('data-range') || 'today';
    var timer = null;
    var polling = false;

    function applyRange(next) {
      range = next || range;
      root.setAttribute('data-range', range);
      if (rangeInput) rangeInput.value = range;

      root.querySelectorAll('[data-ride-tab]').forEach(function (tab) {
        var on = tab.getAttribute('data-ride-tab') === range;
        tab.classList.toggle('is-active', on);
        tab.setAttribute('aria-selected', on ? 'true' : 'false');
      });

      root.querySelectorAll('[data-ride-form-range]').forEach(function (el) {
        el.value = range;
      });

      var empty = root.querySelector('[data-ride-empty]');
      var visibleTotal = 0;
      root.querySelectorAll('[data-ride-col]').forEach(function (col) {
        var count = 0;
        col.querySelectorAll('[data-ride-card]').forEach(function (card) {
          var day = card.getAttribute('data-day') || 'other';
          var show = range === 'all' || day === range;
          card.hidden = !show;
          if (show) count++;
        });
        visibleTotal += count;
        var badge = col.querySelector('[data-ride-col-count]');
        if (badge) badge.textContent = String(count);
        var colEmpty = col.querySelector('[data-ride-col-empty]');
        if (colEmpty) colEmpty.hidden = count > 0;
      });

      if (empty) empty.hidden = !(range === 'today' && visibleTotal === 0);
      var columns = root.querySelector('[data-ride-columns]');
      if (columns) columns.hidden = range === 'today' && visibleTotal === 0;
    }

    function captureScroll() {
      var map = {};
      root.querySelectorAll('[data-ride-col]').forEach(function (col, i) {
        var body = col.querySelector('.book-col-body');
        if (body) map[i] = body.scrollTop;
      });
      return { windowY: window.scrollY, cols: map };
    }

    function restoreScroll(snap) {
      if (!snap) return;
      window.scrollTo(0, snap.windowY);
      root.querySelectorAll('[data-ride-col]').forEach(function (col, i) {
        var body = col.querySelector('.book-col-body');
        if (body && snap.cols[i] != null) body.scrollTop = snap.cols[i];
      });
    }

    async function refreshBoard() {
      if (polling || document.hidden || !refreshHost) return;
      var url = root.getAttribute('data-poll-url');
      if (!url) return;
      polling = true;
      var snap = captureScroll();
      var q = qInput ? qInput.value : '';
      var sep = url.indexOf('?') >= 0 ? '&' : '?';
      var full = url + sep + 'q=' + encodeURIComponent(q || '') + '&range=' + encodeURIComponent(range);
      try {
        var res = await fetch(full, {
          credentials: 'same-origin',
          headers: { Accept: 'text/html', 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!res.ok) return;
        var html = await res.text();
        refreshHost.innerHTML = html;
        applyRange(range);
        restoreScroll(snap);
      } catch (_) {
        /* keep current board on network blip */
      } finally {
        polling = false;
      }
    }

    function startPoll() {
      stopPoll();
      if (document.hidden) return;
      timer = setInterval(refreshBoard, POLL_MS);
    }

    function stopPoll() {
      if (timer) {
        clearInterval(timer);
        timer = null;
      }
    }

    root.addEventListener('click', function (e) {
      var tab = e.target.closest('[data-ride-tab]');
      if (tab && root.contains(tab)) {
        e.preventDefault();
        applyRange(tab.getAttribute('data-ride-tab') || 'today');
        return;
      }

      var call = e.target.closest('[data-ride-call]');
      if (call && root.contains(call)) {
        var phone = call.getAttribute('data-phone');
        if (phone && navigator.clipboard && window.isSecureContext) {
          navigator.clipboard.writeText(phone).catch(function () {});
        }
      }
    });

    root.addEventListener('submit', function (e) {
      var form = e.target;
      if (!(form instanceof HTMLFormElement) || !root.contains(form)) return;
      var msg = form.getAttribute('data-confirm');
      if (msg && !window.confirm(msg)) e.preventDefault();
    });

    document.addEventListener('visibilitychange', function () {
      if (document.hidden) stopPoll();
      else {
        refreshBoard();
        startPoll();
      }
    });

    applyRange(range);
    startPoll();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
