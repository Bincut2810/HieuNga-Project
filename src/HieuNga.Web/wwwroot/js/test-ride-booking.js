/**
 * Test Ride Booking V2 — AJAX submit, spinner, success modal.
 * Single form, no duplicate handlers.
 */
(function () {
  'use strict';

  function boot() {
    const page = document.querySelector('[data-trb-page]');
    const form = document.querySelector('[data-trb-form]');
    if (!page || !form || form.dataset.ready === '1') return;
    form.dataset.ready = '1';

    const submitBtn = form.querySelector('[data-trb-submit]');
    const label = form.querySelector('[data-trb-submit-label]');
    const spinner = form.querySelector('[data-trb-spinner]');
    const errors = form.querySelector('[data-trb-errors]');
    const modal = page.querySelector('[data-trb-modal]');
    const backLink = page.querySelector('[data-trb-back]');
    const motoSelect = form.querySelector('[data-trb-moto]');
    const motoHint = form.querySelector('[data-trb-moto-hint]');
    const motoName = form.querySelector('[data-trb-moto-name]');
    const motoLink = form.querySelector('[data-trb-moto-link]');
    let busy = false;

    function token() {
      const el = form.querySelector('input[name="__RequestVerificationToken"]');
      return el ? el.value : '';
    }

    function setBusy(on) {
      busy = on;
      if (submitBtn) submitBtn.disabled = on;
      if (spinner) spinner.hidden = !on;
      if (label) label.textContent = on ? 'Đang gửi…' : 'Gửi lịch xem xe';
    }

    function showErrors(map) {
      if (!errors) return;
      const lines = [];
      if (map && typeof map === 'object') {
        Object.keys(map).forEach((k) => {
          (map[k] || []).forEach((m) => lines.push(m));
        });
      }
      if (!lines.length) lines.push('Không gửi được. Vui lòng thử lại.');
      errors.hidden = false;
      errors.innerHTML = lines.map((t) => '<p>' + escapeHtml(t) + '</p>').join('');
    }

    function escapeHtml(s) {
      return String(s || '').replace(/[&<>"']/g, (c) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
      })[c]);
    }

    function openModal(motorcycleUrl) {
      if (backLink && motorcycleUrl) backLink.setAttribute('href', motorcycleUrl);
      if (modal) modal.hidden = false;
      document.body.style.overflow = 'hidden';
    }

    function closeModal() {
      if (modal) modal.hidden = true;
      document.body.style.overflow = '';
    }

    modal && modal.querySelectorAll('[data-trb-modal-close]').forEach((el) => {
      el.addEventListener('click', closeModal);
    });

    if (motoSelect) {
      motoSelect.addEventListener('change', () => {
        const opt = motoSelect.selectedOptions[0];
        const slug = opt && opt.getAttribute('data-slug');
        const name = opt && opt.textContent ? opt.textContent.trim() : '';
        if (!motoHint) return;
        if (!slug) {
          motoHint.hidden = true;
          return;
        }
        motoHint.hidden = false;
        if (motoName) motoName.textContent = name;
        if (motoLink) motoLink.setAttribute('href', '/xe/' + slug);
      });
    }

    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      if (busy) return;
      if (errors) {
        errors.hidden = true;
        errors.innerHTML = '';
      }
      setBusy(true);
      try {
        const fd = new FormData(form);
        const res = await fetch(form.getAttribute('data-handler') || '?handler=Book', {
          method: 'POST',
          body: fd,
          credentials: 'same-origin',
          headers: {
            'RequestVerificationToken': token(),
            'Accept': 'application/json'
          }
        });
        const data = await res.json();
        if (!data.success) {
          showErrors(data.errors);
          setBusy(false);
          return;
        }
        openModal(data.motorcycleUrl || '/xe');
        form.reset();
        if (motoHint) motoHint.hidden = true;
        setBusy(false);
      } catch (_) {
        showErrors({ '': ['Lỗi mạng. Vui lòng thử lại.'] });
        setBusy(false);
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.body.addEventListener('htmx:afterSwap', (e) => {
    if (e.detail.target && e.detail.target.id === 'main-content') boot();
  });
})();
