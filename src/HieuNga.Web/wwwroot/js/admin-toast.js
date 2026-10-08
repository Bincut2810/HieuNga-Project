/**
 * Admin Toast + Confirm Modal widget (Phase 1 — Hieu Nga CMS).
 *
 * Public API (window.HieuNgaAdmin):
 *   - toast(message, { type, title, duration })
 *       type: 'success' | 'error' | 'info'  (default: 'info')
 *       duration: ms before auto-dismiss (default: 5000, 0 = sticky)
 *
 *   - confirm({ title, body, confirmText, cancelText, danger })
 *       returns a Promise<boolean>.
 *       Used to replace native window.confirm() with a styled modal.
 *
 * Auto-wiring:
 *   - On DOMContentLoaded, scans for [data-confirm] on submit buttons and
 *     <form data-confirm-form> to intercept the submit and show the modal.
 *   - On DOMContentLoaded, scans for [data-toast] on hidden inputs to fire
 *     a toast from a server-rendered message (used by _AdminFlash).
 *   - Also, any existing [data-admin-flash] success/error banner is mirrored
 *     as a toast on the next page render.
 *
 * No jQuery, no CDN. Vanilla DOM.
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

  var TOAST_DEFAULT_DURATION = 5000;
  var TOAST_DURATION_BY_TYPE = {
    success: 4500,
    error: 0,        // sticky — staff must dismiss explicitly
    info: 5000
  };
  var ICON_BY_TYPE = {
    success: '\u2713',  // ✓
    error: '!',
    info: 'i'
  };

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

  function toast(message, options) {
    options = options || {};
    var type = options.type || 'info';
    var title = options.title || null;
    var duration = options.duration != null
      ? options.duration
      : (TOAST_DURATION_BY_TYPE[type] != null ? TOAST_DURATION_BY_TYPE[type] : TOAST_DEFAULT_DURATION);

    var stack = buildToastStack();
    var el = document.createElement('div');
    el.className = 'admin-toast is-' + type;
    el.setAttribute('role', type === 'error' ? 'alert' : 'status');
    el.innerHTML =
      '<span class="admin-toast-icon" aria-hidden="true">' + esc(ICON_BY_TYPE[type] || 'i') + '</span>' +
      '<div class="admin-toast-body">' +
        (title ? '<p class="admin-toast-title">' + esc(title) + '</p>' : '') +
        '<p class="admin-toast-text">' + esc(message) + '</p>' +
      '</div>' +
      '<button type="button" class="admin-toast-close" aria-label="Đóng">&times;</button>';

    var closeBtn = qs('.admin-toast-close', el);
    function dismiss() {
      if (!el || !el.parentNode) return;
      el.classList.add('is-leaving');
      setTimeout(function () {
        if (el.parentNode) el.parentNode.removeChild(el);
        if (!stack.firstChild) stack.parentNode && stack.parentNode.removeChild(stack);
      }, 220);
    }
    closeBtn.addEventListener('click', dismiss);

    stack.appendChild(el);
    // Animate in next tick so the transition runs.
    requestAnimationFrame(function () {
      el.classList.add('is-shown');
    });

    if (duration > 0) {
      setTimeout(dismiss, duration);
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
    backdrop.innerHTML =
      '<div class="admin-modal" role="document">' +
        '<h2 class="admin-modal-title" data-modal-title></h2>' +
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
    var title = qs('[data-modal-title]', backdrop);
    var body = qs('[data-modal-body]', backdrop);
    var cancelBtn = qs('[data-modal-cancel]', backdrop);
    var confirmBtn = qs('[data-modal-confirm]', backdrop);

    title.textContent = options.title || 'Xác nhận';
    body.textContent = options.body || 'Bạn có chắc?';
    cancelBtn.textContent = options.cancelText || 'Hủy';
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
      function onKey(e) { if (e.key === 'Escape') close(false); }
      confirmBtn.addEventListener('click', onConfirm);
      cancelBtn.addEventListener('click', onCancel);
      backdrop.addEventListener('click', onBackdrop);
      document.addEventListener('keydown', onKey);
      // Focus the cancel by default to keep destructive actions deliberate.
      setTimeout(function () { cancelBtn.focus(); }, 0);
    });
  }

  // ─── Wiring: [data-confirm] on submit buttons + forms ──────────

  function wireConfirmTriggers() {
    qsa('[data-confirm]').forEach(function (el) {
      if (el.dataset.confirmWired === '1') return;
      el.dataset.confirmWired = '1';
      el.addEventListener('click', function (e) {
        // If the element is a submit button inside a form, intercept the
        // submit and show the modal first. The modal promise decides
        // whether to actually submit.
        var form = el.form || el.closest('form');
        if (!form) return;
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
          // Re-submit bypassing this listener. The button retains its name/value
          // and the form posts normally. We avoid re-entering the guard by
          // checking the flag and re-cloning the click programmatically.
          if (el.tagName === 'BUTTON' && el.type === 'submit') {
            form.submit();
          } else if (el.tagName === 'A' && el.getAttribute('href')) {
            window.location.href = el.getAttribute('href');
          }
        });
      });
    });

    // Forms with [data-confirm-form] confirm the whole submit.
    qsa('form[data-confirm-form]').forEach(function (form) {
      if (form.dataset.confirmFormWired === '1') return;
      form.dataset.confirmFormWired = '1';
      form.addEventListener('submit', function (e) {
        // If the form is already inside a [data-confirm] submit button path,
        // the button's listener will handle it. Otherwise (e.g. the user
        // pressed Enter in a field), confirm here.
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

  // ─── Wiring: [data-toast] server-rendered toast trigger ────────

  function wireFlashBanners() {
    qsa('[data-admin-flash]').forEach(function (el) {
      if (el.dataset.toastFired === '1') return;
      el.dataset.toastFired = '1';
      var type = el.dataset.adminFlash === 'success' ? 'success' : 'error';
      var text = (el.textContent || '').trim();
      if (!text) {
        el.style.display = 'none';
        return;
      }
      toast(text, { type: type, title: type === 'success' ? 'Thành công' : 'Có lỗi' });
      el.style.display = 'none';
    });

    // Also support an alternate trigger: hidden <input data-toast value="...">.
    qsa('input[data-toast], div[data-toast-text]').forEach(function (el) {
      if (el.dataset.toastFired === '1') return;
      el.dataset.toastFired = '1';
      var msg = el.value || el.textContent || '';
      if (!msg) return;
      var type = el.dataset.toastType || 'info';
      toast(msg.trim(), { type: type });
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
