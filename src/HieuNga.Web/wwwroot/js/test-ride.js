/**
 * Test Ride Booking — public AJAX submit, success/error modals, confetti.
 */
(function () {
  'use strict';

  var REDUCE = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  function boot() {
    var page = document.querySelector('[data-tr-page]');
    var form = document.querySelector('[data-tr-form]');
    if (!page || !form || form.dataset.ready === '1') return;
    form.dataset.ready = '1';

    var submitBtn = form.querySelector('[data-tr-submit]');
    var label = form.querySelector('[data-tr-submit-label]');
    var spinner = form.querySelector('[data-tr-spinner]');
    var summary = form.querySelector('[data-tr-summary]');
    var successModal = document.querySelector('[data-tr-modal="success"]');
    var errorModal = document.querySelector('[data-tr-modal="error"]');
    var continueLink = document.querySelector('[data-tr-continue]');
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
      if (label) label.textContent = on ? 'Đang gửi…' : 'Đặt lịch xem xe';
    }

    function clearErrors() {
      if (summary) {
        summary.hidden = true;
        summary.innerHTML = '';
      }
      form.querySelectorAll('[data-tr-error]').forEach(function (el) {
        el.hidden = true;
        el.textContent = '';
      });
      form.querySelectorAll('.is-invalid').forEach(function (el) {
        el.classList.remove('is-invalid');
      });
    }

    function showFieldErrors(errors) {
      clearErrors();
      if (!errors) return;
      var lines = [];
      Object.keys(errors).forEach(function (key) {
        var msgs = errors[key] || [];
        var fieldKey = key.replace(/^Input\./, '');
        var box = form.querySelector('[data-tr-error="' + fieldKey + '"]');
        var field = form.querySelector('[data-tr-field="' + fieldKey + '"]');
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

    function escapeHtml(s) {
      return String(s || '').replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
      });
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
      var dialog = modal.querySelector('[data-tr-dialog]');
      if (dialog) requestAnimationFrame(function () { dialog.focus(); });
      onKey = function (e) {
        if (e.key === 'Escape') {
          e.preventDefault();
          closeModal();
        }
      };
      document.addEventListener('keydown', onKey);
    }

    function celebrate() {
      if (REDUCE) return;
      var canvas = document.createElement('canvas');
      canvas.className = 'tr-confetti';
      canvas.setAttribute('aria-hidden', 'true');
      document.body.appendChild(canvas);
      var ctx = canvas.getContext('2d');
      if (!ctx) { canvas.remove(); return; }
      var dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(window.innerWidth * dpr);
      canvas.height = Math.floor(window.innerHeight * dpr);
      canvas.style.width = window.innerWidth + 'px';
      canvas.style.height = window.innerHeight + 'px';
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      var colors = ['#E40521', '#fbbf24', '#34d399', '#60a5fa', '#ffffff'];
      var parts = [];
      var cx = window.innerWidth / 2;
      var cy = window.innerHeight * 0.36;
      for (var i = 0; i < 64; i++) {
        var a = (Math.PI * 2 * i) / 64 + (Math.random() - 0.5) * 0.4;
        var sp = 4 + Math.random() * 6;
        parts.push({
          x: cx, y: cy,
          vx: Math.cos(a) * sp * (0.6 + Math.random()),
          vy: Math.sin(a) * sp - 5,
          w: 4 + Math.random() * 4,
          h: 6 + Math.random() * 6,
          rot: Math.random() * Math.PI,
          vr: (Math.random() - 0.5) * 0.3,
          color: colors[i % colors.length]
        });
      }
      var start = performance.now();
      var dur = 1800;
      var raf = 0;
      function frame(now) {
        var t = now - start;
        if (t >= dur) { cancelAnimationFrame(raf); canvas.remove(); return; }
        var fade = 1 - t / dur;
        ctx.clearRect(0, 0, window.innerWidth, window.innerHeight);
        for (var j = 0; j < parts.length; j++) {
          var p = parts[j];
          p.vy += 0.22; p.x += p.vx; p.y += p.vy; p.vx *= 0.992; p.rot += p.vr;
          ctx.save();
          ctx.translate(p.x, p.y);
          ctx.rotate(p.rot);
          ctx.globalAlpha = fade;
          ctx.fillStyle = p.color;
          ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
          ctx.restore();
        }
        raf = requestAnimationFrame(frame);
      }
      raf = requestAnimationFrame(frame);
      setTimeout(function () {
        cancelAnimationFrame(raf);
        if (canvas.parentNode) canvas.remove();
      }, dur + 60);
    }

    function showSuccess(data) {
      var received = document.querySelector('[data-tr-received]');
      var meta = document.querySelector('[data-tr-success-meta]');
      var msg = document.querySelector('[data-tr-success-msg]');
      var now = new Date();
      var dd = String(now.getDate()).padStart(2, '0');
      var mm = String(now.getMonth() + 1).padStart(2, '0');
      var yyyy = now.getFullYear();
      var hh = String(now.getHours()).padStart(2, '0');
      var mi = String(now.getMinutes()).padStart(2, '0');
      if (received) {
        received.textContent = 'Đã ghi nhận lúc ' + hh + ':' + mi + ' ' + dd + '/' + mm + '/' + yyyy;
      }
      if (meta) {
        meta.innerHTML =
          '<p><strong>' + escapeHtml(data.customerName || '') + '</strong></p>' +
          '<p>' + escapeHtml(data.motorcycleName || '') + '</p>' +
          '<p>' + escapeHtml(data.appointmentDate || '') + ' · ' + escapeHtml(data.appointmentTime || '') + '</p>';
      }
      if (msg) {
        msg.textContent = data.isDuplicate
          ? (data.message || 'Bạn đã gửi lịch hẹn trước đó. Nhân viên sẽ sớm liên hệ với bạn.')
          : 'Cảm ơn bạn đã đăng ký. Nhân viên sẽ sớm liên hệ xác nhận lịch hẹn.';
      }
      if (continueLink && data.motorcycleUrl) {
        continueLink.setAttribute('href', data.motorcycleUrl);
      }
      openModal(successModal);
      celebrate();
    }

    function showErrorModal(text) {
      var body = document.querySelector('[data-tr-error-body]');
      if (body) body.textContent = text || 'Đã xảy ra lỗi. Vui lòng thử lại hoặc gọi hotline.';
      openModal(errorModal);
    }

    document.querySelectorAll('[data-tr-close]').forEach(function (el) {
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
      fetch('/dat-lich-lai-thu?handler=Book', {
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
          showSuccess(data);
        })
        .catch(function () {
          showErrorModal('Lỗi mạng. Vui lòng kiểm tra kết nối và thử lại.');
          setBusy(false);
        });
    });
  }

  window.bootTestRide = boot;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.body.addEventListener('htmx:afterSwap', function (e) {
    if (e.detail && e.detail.target && e.detail.target.id === 'main-content') boot();
  });
})();
