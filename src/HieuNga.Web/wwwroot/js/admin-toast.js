/**
 * Admin Toast + Confirm Modal widget (Phase 2 — Hieu Nga CMS).
 *
 * Public API (window.HieuNgaAdmin):
 *   - toast(message, { type, title, duration, sticky })
 *       type     : 'success' | 'error' | 'info' (default 'info')
 *       title    : optional bold caption rendered above the message
 *       duration : ms before auto-dismiss (default: 4500 success / 0 error)
 *       sticky   : true → manual dismiss only (overrides duration)
 *
 *   - confirm({ title, body, confirmText, cancelText, danger })
 *       returns a Promise<boolean> — the styled replacement for native
 *       window.confirm(). Used by the data-confirm widgets.
 *
 * Auto-wiring (DOMContentLoaded):
 *   - Scans for [data-toast] hidden inputs / [data-toast-text] divs and
 *     emits a toast from the TempData payload written by _AdminFlash.
 *   - Scans for [data-admin-flash] banners and mirrors them as toasts
 *     (then hides the inline banner so JS users don't see duplicates).
 *   - Scans for [data-confirm] (submit button or form) and intercepts
 *     submit until the modal confirms.
 *
 * Accessibility:
 *   - Toasts use role=status for success/info and role=alert for errors.
 *   - aria-live="polite" for success, aria-live="assertive" for errors.
 *   - Close buttons have aria-label="Đóng".
 *   - prefers-reduced-motion disables the slide animation.
 *
 * No jQuery, no CDN. Vanilla DOM + inline SVG icons.
 */
(function () {
  'use strict';

  function qs(sel, root) { return (root || document).querySelector(sel); }
  function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  // Default timings: errors are sticky (staff must dismiss), success auto-dismisses.
  var TOAST_DURATION_BY_TYPE = {
    success: 4500,
    info:    5000,
    error:   0   // sticky
  };
  var TOAST_DEFAULT_DURATION = 5000;

  // Inline SVG icons — no external library.
  var ICON_SVG = {
    success:
      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" ' +
      'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<polyline points="20 6 9 17 4 12"/></svg>',
    error:
      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" ' +
      'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<line x1="12" y1="8" x2="12" y2="13"/>' +
      '<circle cx="12" cy="17.2" r="1.05" fill="currentColor"/></svg>',
    info:
      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" ' +
      'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<line x1="12" y1="11" x2="12" y2="16"/>' +
      '<circle cx="12" cy="7.6" r="1.05" fill="currentColor"/></svg>'
  };
  var TITLE_BY_TYPE = {
    success: 'Thành công',
    error:   'Có lỗi',
    info:    'Thông báo'
  };
  var CLOSE_SVG =
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" ' +
    'stroke-linecap="round" aria-hidden="true">' +
    '<line x1="6" y1="6" x2="18" y2="18"/>' +
    '<line x1="18" y1="6" x2="6" y2="18"/></svg>';

  function reducedMotion() {
    try {
      return window.matchMedia &&
             window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    } catch (e) { return false; }
  }

  function buildToastStack() {
    var stack = qs('.admin-toast-stack');
    if (stack) return stack;
    stack = document.createElement('div');
    stack.className = 'admin-toast-stack';
    stack.setAttribute('role', 'region');
    stack.setAttribute('aria-label', 'Thông báo');
    document.body.appendChild(stack);
    return stack;
  }

  /**
   * Render and show a single toast.
   * Returns { dismiss(), element } so callers can manually close it.
   */
  function toast(message, options) {
    options = options || {};
    var type   = options.type || 'info';
    var title  = options.title || (options.title === null ? null : TITLE_BY_TYPE[type] || TITLE_BY_TYPE.info);
    var duration = options.duration != null
      ? options.duration
      : (options.sticky ? 0 : (TOAST_DURATION_BY_TYPE[type] != null ? TOAST_DURATION_BY_TYPE[type] : TOAST_DEFAULT_DURATION));
    var isSticky = !!options.sticky || duration <= 0;

    var stack = buildToastStack();
    var el = document.createElement('div');
    el.className = 'admin-toast is-' + type + (isSticky ? ' is-sticky' : '');
    el.setAttribute('role', type === 'error' ? 'alert' : 'status');
    el.setAttribute('aria-live', type === 'error' ? 'assertive' : 'polite');
    if (isSticky) el.setAttribute('data-sticky', 'true');

    el.innerHTML =
      '<span class="admin-toast-icon" aria-hidden="true">' + (ICON_SVG[type] || ICON_SVG.info) + '</span>' +
      '<div class="admin-toast-body">' +
        (title ? '<p class="admin-toast-title">' + esc(title) + '</p>' : '') +
        '<p class="admin-toast-text">' + esc(message) + '</p>' +
      '</div>' +
      '<button type="button" class="admin-toast-close" aria-label="Đóng">' + CLOSE_SVG + '</button>';

    var closeBtn = qs('.admin-toast-close', el);
    var dismissed = false;
    var timer = null;

    function dismiss() {
      if (dismissed) return;
      dismissed = true;
      if (timer) { clearTimeout(timer); timer = null; }
      if (!el || !el.parentNode) return;
      el.classList.add('is-leaving');
      setTimeout(function () {
        if (el.parentNode) el.parentNode.removeChild(el);
        // Keep the stack element in DOM (so role=region stays discoverable)
        // but make sure pointer-events stays none. The :empty CSS rule also
        // hides it once emptied.
      }, 240);
    }
    closeBtn.addEventListener('click', dismiss);

    stack.appendChild(el);
    // Animate in next tick so the transition runs.
    requestAnimationFrame(function () {
      el.classList.add('is-shown');
    });

    if (!isSticky && !reducedMotion()) {
      timer = setTimeout(dismiss, duration);
    }

    return { dismiss: dismiss, element: el };
  }

  // ─── Confirm modal ─────────────────────────────────────────────

  function buildModal() {
    var backdrop = qs('.admin-modal-backdrop');
    if (backdrop) return backdrop;
    backdrop = document.createElement('div');
    backdrop.className = 'admin-modal-backdrop';
    backdrop.setAttribute('role', 'dialog');
    backdrop.setAttribute('aria-modal', 'true');
    backdrop.setAttribute('aria-labelledby', 'admin-modal-title');
    backdrop.innerHTML =
      '<div class="admin-modal" role="document">' +
        '<h2 id="admin-modal-title" class="admin-modal-title" data-modal-title></h2>' +
        '<p class="admin-modal-body" data-modal-body></p>' +
        '<div class="admin-modal-actions">' +
          '<button type="button" class="admin-btn admin-btn-ghost" data-modal-cancel></button>' +
          '<button type="button" class="admin-btn admin-btn-primary" data-modal-confirm></button>' +
        '</div>' +
      '</div>';
    document.body.appendChild(backdrop);
    return backdrop;
  }

  function confirmDialog(options) {
    options = options || {};
    var backdrop = buildModal();
    var title    = qs('[data-modal-title]', backdrop);
    var body     = qs('[data-modal-body]', backdrop);
    var cancelBtn  = qs('[data-modal-cancel]', backdrop);
    var confirmBtn = qs('[data-modal-confirm]', backdrop);

    title.textContent = options.title || 'Xác nhận';
    body.textContent  = options.body  || 'Bạn có chắc muốn tiếp tục?';
    cancelBtn.textContent  = options.cancelText  || 'Hủy';
    confirmBtn.textContent = options.confirmText || 'Xác nhận';

    confirmBtn.classList.remove('admin-btn-primary', 'admin-btn-danger');
    confirmBtn.classList.add(options.danger ? 'admin-btn-danger' : 'admin-btn-primary');

    backdrop.classList.add('is-open');

    return new Promise(function (resolve) {
      function close(result) {
        backdrop.classList.remove('is-open');
        confirmBtn.removeEventListener('click', onConfirm);
        cancelBtn.removeEventListener('click', onCancel);
        backdrop.removeEventListener('click', onBackdrop);
        document.removeEventListener('keydown', onKey);
        resolve(result);
      }
      function onConfirm() { close(true); }
      function onCancel() { close(false); }
      function onBackdrop(e) { if (e.target === backdrop) close(false); }
      function onKey(e) {
        if (e.key === 'Escape') close(false);
        else if (e.key === 'Enter') close(true);
      }
      confirmBtn.addEventListener('click', onConfirm);
      cancelBtn.addEventListener('click', onCancel);
      backdrop.addEventListener('click', onBackdrop);
      document.addEventListener('keydown', onKey);
      // Focus the cancel button by default to keep destructive actions
      // deliberate (Escape stays available to abort).
      setTimeout(function () { cancelBtn.focus(); }, 0);
    });
  }

  // ─── Wiring: [data-confirm] on submit buttons / [data-confirm-form] on forms

  function wireConfirmTriggers() {
    qsa('[data-confirm]').forEach(function (el) {
      if (el.dataset.confirmWired === '1') return;
      el.dataset.confirmWired = '1';
      el.addEventListener('click', function (e) {
        var form = el.form || el.closest('form');
        if (!form) return;
        // Skip if the user pressed the button while JS is mid-shutdown.
        e.preventDefault();
        e.stopPropagation();
        var opts = {
          title: el.dataset.confirmTitle || 'Xác nhận',
          body: el.dataset.confirmBody || 'Bạn có chắc muốn tiếp tục?',
          confirmText: el.dataset.confirmOk || 'Xác nhận',
          cancelText: el.dataset.confirmCancel || 'Hủy',
          danger: el.dataset.confirmDanger === 'true'
        };
        confirmDialog(opts).then(function (ok) {
          if (!ok) return;
          // Re-submit bypassing this listener. The button retains its
          // name/value and the form posts normally.
          if (el.tagName === 'BUTTON' && el.type === 'submit') {
            form.submit();
          } else if (el.tagName === 'A' && el.getAttribute('href')) {
            window.location.href = el.getAttribute('href');
          }
        });
      });
    });

    // Forms with [data-confirm-form] confirm the whole submit (e.g. when
    // the user pressed Enter inside a field instead of clicking the
    // button).
    qsa('form[data-confirm-form]').forEach(function (form) {
      if (form.dataset.confirmFormWired === '1') return;
      form.dataset.confirmFormWired = '1';
      form.addEventListener('submit', function (e) {
        if (e.defaultPrevented) return;
        e.preventDefault();
        var opts = {
          title: form.dataset.confirmTitle || 'Xác nhận',
          body: form.dataset.confirmBody || 'Bạn có chắc muốn tiếp tục?',
          confirmText: form.dataset.confirmOk || 'Xác nhận',
          cancelText: form.dataset.confirmCancel || 'Hủy',
          danger: form.dataset.confirmDanger === 'true'
        };
        confirmDialog(opts).then(function (ok) {
          if (!ok) return;
          form.submit();
        });
      });
    });
  }

  // ─── Wiring: server-rendered toast triggers + fallback flash banners

  function wireFlashBanners() {
    // Banner fallback (admin-flash-*). After the toast fires, hide the
    // inline banner so JS users don't see the message twice.
    qsa('[data-admin-flash]').forEach(function (el) {
      if (el.dataset.toastFired === '1') return;
      el.dataset.toastFired = '1';
      var type = el.dataset.adminFlash === 'success' ? 'success'
               : el.dataset.adminFlash === 'error'   ? 'error'
               : 'info';
      var raw = (el.textContent || '').trim();
      // Strip the legacy "✓ " prefix added by _AdminFlash.
      raw = raw.replace(/^\s*[✓✔]\s*/, '').trim();
      if (!raw) {
        el.style.display = 'none';
        return;
      }
      toast(raw, { type: type });
      el.style.display = 'none';
    });

    // Hidden toast triggers (data-toast on <input>, data-toast-text on <div>).
    qsa('input[data-toast], div[data-toast-text]').forEach(function (el) {
      if (el.dataset.toastFired === '1') return;
      el.dataset.toastFired = '1';
      var raw = el.value || el.textContent || '';
      if (!raw) return;
      var type = el.dataset.toastType || 'info';
      var text = raw.replace(/^\s*[✓✔]\s*/, '').trim();
      if (!text) return;
      toast(text, { type: type });
    });
  }

  // ─── Expose ────────────────────────────────────────────────────

  function init() {
    wireFlashBanners();
    wireConfirmTriggers();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.HieuNgaAdmin = window.HieuNgaAdmin || {};
  window.HieuNgaAdmin.toast = toast;
  window.HieuNgaAdmin.confirm = confirmDialog;
})();
