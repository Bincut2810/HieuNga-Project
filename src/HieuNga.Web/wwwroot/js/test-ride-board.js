/**
 * Test Ride board — Today/Tomorrow/All tabs (client filter), confirm dialogs, tap-to-call copy.
 */
(function () {
  'use strict';

  function boot() {
    const root = document.querySelector('[data-ride-board]');
    if (!root || root.dataset.ready === '1') return;
    root.dataset.ready = '1';

    const empty = root.querySelector('[data-ride-empty]');
    const rangeInput = root.querySelector('[data-ride-range-input]');
    let range = root.getAttribute('data-range') || 'today';

    function applyRange(next) {
      range = next;
      root.setAttribute('data-range', range);
      if (rangeInput) rangeInput.value = range;

      root.querySelectorAll('[data-ride-tab]').forEach((tab) => {
        const on = tab.getAttribute('data-ride-tab') === range;
        tab.classList.toggle('is-active', on);
        tab.setAttribute('aria-selected', on ? 'true' : 'false');
      });

      root.querySelectorAll('[data-ride-form-range]').forEach((el) => {
        el.value = range;
      });

      let visibleTotal = 0;
      root.querySelectorAll('[data-ride-col]').forEach((col) => {
        let count = 0;
        col.querySelectorAll('[data-ride-card]').forEach((card) => {
          const day = card.getAttribute('data-day') || 'other';
          const show = range === 'all' || day === range;
          card.hidden = !show;
          if (show) count++;
        });
        visibleTotal += count;
        const badge = col.querySelector('[data-ride-col-count]');
        if (badge) badge.textContent = String(count);
        const colEmpty = col.querySelector('[data-ride-col-empty]');
        if (colEmpty) colEmpty.hidden = count > 0;
      });

      if (empty) {
        empty.hidden = !(range === 'today' && visibleTotal === 0);
      }
      const columns = root.querySelector('[data-ride-columns]');
      if (columns) columns.hidden = range === 'today' && visibleTotal === 0;
    }

    root.querySelectorAll('[data-ride-tab]').forEach((tab) => {
      tab.addEventListener('click', () => applyRange(tab.getAttribute('data-ride-tab') || 'today'));
    });

    root.querySelectorAll('form[data-confirm]').forEach((form) => {
      form.addEventListener('submit', (e) => {
        const msg = form.getAttribute('data-confirm');
        if (msg && !window.confirm(msg)) e.preventDefault();
      });
    });

    root.querySelectorAll('[data-ride-call]').forEach((el) => {
      el.addEventListener('click', () => {
        const phone = el.getAttribute('data-phone');
        if (!phone) return;
        if (navigator.clipboard && window.isSecureContext) {
          navigator.clipboard.writeText(phone).catch(() => {});
        }
      });
    });

    applyRange(range);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
