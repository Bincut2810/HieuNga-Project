/**
 * Test Ride Booking — AJAX submit, double-submit guard, success/error modals, confetti.
 * Lifecycle owned by polish.js via window.bootTestRideBooking (no @section Scripts).
 */
(function () {
  'use strict';

  var REDUCE = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var DEFAULT_DESC =
    'Cảm ơn bạn đã đăng ký lịch xem xe.<br />Nhân viên Xe máy Hiếu Nga sẽ liên hệ với bạn trong thời gian sớm nhất để xác nhận lịch hẹn.';

  function boot() {
    var page = document.querySelector('[data-trb-page]');
    var form = document.querySelector('[data-trb-form]');
    if (!page || !form || form.dataset.ready === '1') return;
    form.dataset.ready = '1';

    var submitBtn = form.querySelector('[data-trb-submit]');
    var label = form.querySelector('[data-trb-submit-label]');
    var spinner = form.querySelector('[data-trb-spinner]');
    var successModal = page.querySelector('[data-trb-modal="success"]');
    var errorModal = page.querySelector('[data-trb-modal="error"]');
    var errorBody = page.querySelector('[data-trb-error-body]');
    var successDesc = page.querySelector('[data-trb-success-desc]');
    var received = page.querySelector('[data-trb-received]');
    var receivedDate = page.querySelector('[data-trb-received-date]');
    var receivedTime = page.querySelector('[data-trb-received-time]');
    var backLink = page.querySelector('[data-trb-back]');
    var callLink = page.querySelector('[data-trb-call]');
    var motoSelect = form.querySelector('[data-trb-moto]');
    var motoHint = form.querySelector('[data-trb-moto-hint]');
    var motoName = form.querySelector('[data-trb-moto-name]');
    var motoLink = form.querySelector('[data-trb-moto-link]');
    var busy = false;
    var activeModal = null;
    var lastFocus = null;
    var keyHandler = null;
    var defaultTime = form.querySelector('#PreferredTime')
      ? form.querySelector('#PreferredTime').value
      : '';

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
      if (label) label.textContent = on ? 'Đang gửi…' : 'Gửi lịch hẹn';
    }

    function resetFormState() {
      form.reset();
      if (motoHint) motoHint.hidden = true;
      setBusy(false);
      if (successDesc) successDesc.innerHTML = DEFAULT_DESC;
      if (received) received.hidden = true;
      form.querySelectorAll('.is-invalid').forEach(function (el) {
        el.classList.remove('is-invalid');
      });
    }

    function escapeHtml(s) {
      return String(s || '').replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
      });
    }

    function collectErrors(map) {
      var lines = [];
      if (map && typeof map === 'object') {
        Object.keys(map).forEach(function (k) {
          (map[k] || []).forEach(function (m) { lines.push(m); });
        });
      }
      if (!lines.length) lines.push('Không gửi được lịch. Vui lòng thử lại.');
      return lines;
    }

    function focusables(root) {
      return Array.prototype.slice.call(
        root.querySelectorAll('a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])')
      ).filter(function (el) {
        return !el.hasAttribute('disabled') && el.offsetParent !== null;
      });
    }

    function closeModal(options) {
      var shouldReset = options && options.reset;
      if (!activeModal) {
        if (shouldReset) resetFormState();
        return;
      }
      var wasSuccess = activeModal.getAttribute('data-trb-modal') === 'success';
      activeModal.hidden = true;
      activeModal = null;
      document.body.style.overflow = '';
      document.body.classList.remove('trb-modal-open');
      if (keyHandler) {
        document.removeEventListener('keydown', keyHandler);
        keyHandler = null;
      }
      if (lastFocus && typeof lastFocus.focus === 'function') {
        try { lastFocus.focus(); } catch (_) { /* ignore */ }
      }
      lastFocus = null;
      if (shouldReset || wasSuccess) resetFormState();
    }

    function openModal(modal) {
      if (!modal) return;
      closeModal({ reset: false });
      lastFocus = document.activeElement;
      activeModal = modal;
      modal.hidden = false;
      document.body.style.overflow = 'hidden';
      document.body.classList.add('trb-modal-open');

      var dialog = modal.querySelector('[data-trb-dialog]');
      var nodes = focusables(modal);
      var first = nodes[0] || dialog;
      if (first) {
        requestAnimationFrame(function () { first.focus(); });
      }

      keyHandler = function (e) {
        if (e.key === 'Escape') {
          e.preventDefault();
          closeModal({ reset: true });
          return;
        }
        if (e.key !== 'Tab' || !dialog) return;
        var list = focusables(modal);
        if (!list.length) {
          e.preventDefault();
          dialog.focus();
          return;
        }
        var i = list.indexOf(document.activeElement);
        if (e.shiftKey) {
          if (i <= 0) {
            e.preventDefault();
            list[list.length - 1].focus();
          }
        } else if (i === list.length - 1) {
          e.preventDefault();
          list[0].focus();
        }
      };
      document.addEventListener('keydown', keyHandler);
    }

    function showError(map) {
      var lines = collectErrors(map);
      if (errorBody) {
        errorBody.innerHTML = lines.map(function (t) {
          return '<p>' + escapeHtml(t) + '</p>';
        }).join('');
      }
      openModal(errorModal);
    }

    function stampReceived() {
      var now = new Date();
      var dd = String(now.getDate()).padStart(2, '0');
      var mm = String(now.getMonth() + 1).padStart(2, '0');
      var yyyy = now.getFullYear();
      var hh = String(now.getHours()).padStart(2, '0');
      var mi = String(now.getMinutes()).padStart(2, '0');
      if (receivedDate) receivedDate.textContent = dd + '/' + mm + '/' + yyyy;
      if (receivedTime) receivedTime.textContent = hh + ':' + mi;
      if (received) received.hidden = false;
    }

    function showSuccess(motorcycleUrl, message) {
      if (backLink && motorcycleUrl) backLink.setAttribute('href', motorcycleUrl);
      if (successDesc) {
        if (message && message.indexOf('đã gửi lịch hẹn trước đó') >= 0) {
          successDesc.textContent = message;
        } else {
          successDesc.innerHTML = DEFAULT_DESC;
        }
      }
      stampReceived();
      openModal(successModal);
      celebrate();
    }

    function celebrate() {
      if (REDUCE) return;
      var canvas = document.createElement('canvas');
      canvas.className = 'trb-confetti';
      canvas.setAttribute('aria-hidden', 'true');
      document.body.appendChild(canvas);
      var ctx = canvas.getContext('2d');
      if (!ctx) {
        canvas.remove();
        return;
      }

      var dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(window.innerWidth * dpr);
      canvas.height = Math.floor(window.innerHeight * dpr);
      canvas.style.width = window.innerWidth + 'px';
      canvas.style.height = window.innerHeight + 'px';
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

      var colors = ['#E40521', '#fbbf24', '#34d399', '#60a5fa', '#f472b6', '#ffffff'];
      var parts = [];
      var cx = window.innerWidth / 2;
      var cy = window.innerHeight * 0.38;
      for (var i = 0; i < 72; i++) {
        var angle = (Math.PI * 2 * i) / 72 + (Math.random() - 0.5) * 0.4;
        var speed = 4 + Math.random() * 7;
        parts.push({
          x: cx,
          y: cy,
          vx: Math.cos(angle) * speed * (0.6 + Math.random()),
          vy: Math.sin(angle) * speed - 6,
          w: 4 + Math.random() * 5,
          h: 6 + Math.random() * 8,
          rot: Math.random() * Math.PI,
          vr: (Math.random() - 0.5) * 0.35,
          color: colors[i % colors.length]
        });
      }

      var start = performance.now();
      var duration = 1800;
      var raf = 0;
      function frame(now) {
        var t = now - start;
        if (t >= duration) {
          cancelAnimationFrame(raf);
          canvas.remove();
          return;
        }
        var fade = 1 - t / duration;
        ctx.clearRect(0, 0, window.innerWidth, window.innerHeight);
        for (var j = 0; j < parts.length; j++) {
          var p = parts[j];
          p.vy += 0.22;
          p.x += p.vx;
          p.y += p.vy;
          p.vx *= 0.992;
          p.rot += p.vr;
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
      }, duration + 80);
    }

    page.querySelectorAll('[data-trb-modal-close]').forEach(function (el) {
      el.addEventListener('click', function (e) {
        e.preventDefault();
        closeModal({ reset: true });
      });
    });

    if (backLink) {
      backLink.addEventListener('click', function () {
        resetFormState();
      });
    }

    if (motoSelect) {
      motoSelect.addEventListener('change', function () {
        var opt = motoSelect.selectedOptions[0];
        var slug = opt && opt.getAttribute('data-slug');
        var name = opt && opt.textContent ? opt.textContent.trim() : '';
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

    if (callLink && page.getAttribute('data-tel')) {
      callLink.setAttribute('href', page.getAttribute('data-tel'));
    }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      e.stopPropagation();
      if (busy) return;

      setBusy(true);
      var fd = new FormData(form);
      var url = form.getAttribute('data-handler') || '/dat-lich-lai-thu?handler=Book';

      fetch(url, {
        method: 'POST',
        body: fd,
        credentials: 'same-origin',
        headers: {
          'RequestVerificationToken': token(),
          'Accept': 'application/json'
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
          if (!result.data || !result.data.success) {
            showError(result.data && result.data.errors);
            setBusy(false);
            return;
          }
          var motoUrl = result.data.motorcycleUrl
            || page.getAttribute('data-back-url')
            || '/xe';
          form.reset();
          if (defaultTime && form.querySelector('#PreferredTime')) {
            form.querySelector('#PreferredTime').value = defaultTime;
          }
          if (motoHint) motoHint.hidden = true;
          setBusy(false);
          showSuccess(motoUrl, result.data.message);
        })
        .catch(function () {
          showError({ '': ['Lỗi mạng. Vui lòng kiểm tra kết nối và thử lại.'] });
          setBusy(false);
        });
    });
  }

  window.bootTestRideBooking = boot;
})();
