/**
 * Maintenance booking — same UX patterns as Test Ride (AJAX, modals, busy state).
 */
(function () {
  'use strict';

  function boot() {
    var page = document.querySelector('[data-maint-page]');
    var form = document.querySelector('[data-maint-form]');
    if (!page || !form || form.dataset.ready === '1') return;
    form.dataset.ready = '1';

    var submitBtn = form.querySelector('[data-maint-submit]');
    var label = form.querySelector('[data-maint-submit-label]');
    var spinner = form.querySelector('[data-maint-spinner]');
    var summary = form.querySelector('[data-maint-summary]');
    var successModal = document.querySelector('[data-maint-modal="success"]');
    var errorModal = document.querySelector('[data-maint-modal="error"]');
    var busy = false;
    var active = null;
    var lastFocus = null;
    var onKey = null;

    function token() {
      var el = form.querySelector('input[name="__RequestVerificationToken"]');
      return el ? el.value : '';
    }

    function setBusy(on) {
      busy = on;
      if (submitBtn) {
        submitBtn.disabled = on;
        submitBtn.setAttribute('aria-busy', on ? 'true' : 'false');
      }
      if (spinner) spinner.hidden = !on;
      var idle = (submitBtn && submitBtn.getAttribute('data-tr-idle-label')) || 'Đặt lịch bảo dưỡng';
      if (label) label.textContent = on ? 'Đang gửi…' : idle;
    }

    function clearErrors() {
      if (summary) {
        summary.hidden = true;
        summary.innerHTML = '';
      }
      form.querySelectorAll('[data-maint-error]').forEach(function (el) {
        el.hidden = true;
        el.textContent = '';
      });
      form.querySelectorAll('.is-invalid').forEach(function (el) {
        el.classList.remove('is-invalid');
      });
    }

    function escapeHtml(s) {
      return String(s || '').replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
      });
    }

    function showFieldErrors(errors) {
      clearErrors();
      if (!errors) return;
      var lines = [];
      Object.keys(errors).forEach(function (key) {
        var msgs = errors[key] || [];
        var box = form.querySelector('[data-maint-error="' + key + '"]');
        var field = form.querySelector('[data-maint-field="' + key + '"]');
        if (field) field.classList.add('is-invalid');
        if (box && msgs.length) {
          box.textContent = msgs[0];
          box.hidden = false;
        }
        msgs.forEach(function (m) { lines.push(m); });
      });
      if (summary && lines.length) {
        summary.hidden = false;
        summary.innerHTML = lines.map(function (t) {
          return '<p>' + escapeHtml(t) + '</p>';
        }).join('');
      }
    }

    function closeModal() {
      if (!active) return;
      active.hidden = true;
      active = null;
      document.body.classList.remove('tr-modal-open');
      if (onKey) {
        document.removeEventListener('keydown', onKey);
        onKey = null;
      }
      if (lastFocus && lastFocus.focus) {
        try { lastFocus.focus(); } catch (_) {}
      }
      lastFocus = null;
    }

    function openModal(modal) {
      if (!modal) return;
      closeModal();
      lastFocus = document.activeElement;
      active = modal;
      modal.hidden = false;
      document.body.classList.add('tr-modal-open');
      var dialog = modal.querySelector('[data-maint-dialog]');
      if (dialog) requestAnimationFrame(function () { dialog.focus(); });
      onKey = function (e) {
        if (e.key === 'Escape') {
          e.preventDefault();
          closeModal();
        }
      };
      document.addEventListener('keydown', onKey);
    }

    function showErrorModal(text) {
      var body = document.querySelector('[data-maint-error-body]');
      if (body) body.textContent = text || 'Đã xảy ra lỗi. Vui lòng thử lại hoặc gọi hotline.';
      openModal(errorModal);
    }

    document.querySelectorAll('[data-maint-close]').forEach(function (el) {
      el.addEventListener('click', function (e) {
        e.preventDefault();
        closeModal();
      });
    });

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      e.stopPropagation();
      if (busy) return;
      clearErrors();
      setBusy(true);

      var fd = new FormData(form);
      fetch('/bao-duong?handler=Book', {
        method: 'POST',
        body: fd,
        credentials: 'same-origin',
        headers: {
          RequestVerificationToken: token(),
          Accept: 'application/json'
        }
      })
        .then(function (res) {
          return res.json().then(function (data) {
            return { ok: res.ok, data: data };
          }).catch(function () {
            return { ok: false, data: null };
          });
        })
        .then(function (result) {
          var data = result.data;
          if (!data || !data.success) {
            if (data && data.errors) showFieldErrors(data.errors);
            else showErrorModal(data && data.message);
            setBusy(false);
            return;
          }
          form.reset();
          setBusy(false);
          openModal(successModal);
        })
        .catch(function () {
          showErrorModal('Lỗi mạng. Vui lòng kiểm tra kết nối và thử lại.');
          setBusy(false);
        });
    });
  }

  window.bootMaintenanceBooking = boot;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.body.addEventListener('htmx:afterSwap', function (e) {
    if (e.detail && e.detail.target && e.detail.target.id === 'main-content') boot();
  });
})();
