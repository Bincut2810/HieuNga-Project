/**
 * Booking Center — unified drawer (one Detail endpoint for all booking kinds).
 */
(function () {
  'use strict';

  var STATUS = {
    0: 'Đang chờ',
    1: 'Đã đến',
    2: 'Hoàn thành',
    3: 'Đã hủy'
  };

  function boot() {
    var page = document.querySelector('[data-bc-page]');
    var drawer = document.querySelector('[data-bc-drawer]');
    if (!page || !drawer || page.dataset.ready === '1') return;
    page.dataset.ready = '1';

    var body = drawer.querySelector('[data-bc-drawer-body]');
    var form = drawer.querySelector('[data-bc-drawer-form]');
    var idInput = drawer.querySelector('[data-bc-drawer-id]');
    var statusSelect = drawer.querySelector('[data-bc-drawer-status]');
    var notes = drawer.querySelector('[data-bc-drawer-notes]');
    var panel = drawer.querySelector('[data-bc-drawer-panel]');
    var lastFocus = null;

    function close() {
      drawer.hidden = true;
      document.body.classList.remove('bc-drawer-open');
      if (lastFocus && lastFocus.focus) {
        try { lastFocus.focus(); } catch (_) {}
      }
      lastFocus = null;
    }

    function open() {
      lastFocus = document.activeElement;
      drawer.hidden = false;
      document.body.classList.add('bc-drawer-open');
      if (panel) requestAnimationFrame(function () { panel.focus(); });
    }

    function esc(s) {
      return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
      });
    }

    function fillStatusOptions(current) {
      if (!statusSelect) return;
      var cur = typeof current === 'string' ? current : Number(current);
      var map = [
        { v: 0, l: 'Đang chờ' },
        { v: 1, l: 'Đã đến' },
        { v: 2, l: 'Hoàn thành' },
        { v: 3, l: 'Đã hủy' }
      ];
      var allowed = [cur];
      if (cur === 0) allowed = [0, 1, 2, 3];
      else if (cur === 1) allowed = [1, 2, 3];
      else if (cur === 2) allowed = [2];
      else if (cur === 3) allowed = [3];

      statusSelect.innerHTML = '';
      map.forEach(function (opt) {
        if (allowed.indexOf(opt.v) < 0) return;
        var o = document.createElement('option');
        o.value = String(opt.v);
        o.textContent = opt.l;
        if (opt.v === cur) o.selected = true;
        statusSelect.appendChild(o);
      });
    }

    function renderBody(data) {
      if (!body) return;
      var created = data.createdAt
        ? new Date(data.createdAt).toLocaleString('vi-VN')
        : '—';
      body.innerHTML =
        '<dl class="bc-dl">' +
        '<div><dt>Khách hàng</dt><dd>' + esc(data.customerName) + '</dd></div>' +
        '<div><dt>Điện thoại</dt><dd><a href="tel:' + esc(data.phoneNumber) + '">' +
          esc(data.phoneNumber) + '</a></dd></div>' +
        '<div><dt>Xe</dt><dd>' + esc(data.vehicle) + '</dd></div>' +
        '<div><dt>Dịch vụ</dt><dd>' + esc(data.service || '—') + '</dd></div>' +
        '<div><dt>Chi nhánh</dt><dd>' + esc(data.branch || '—') + '</dd></div>' +
        '<div><dt>Ngày hẹn</dt><dd>' + esc(data.appointmentDate) + '</dd></div>' +
        '<div><dt>Giờ hẹn</dt><dd>' + esc(data.appointmentTime) + '</dd></div>' +
        '<div><dt>Nguồn lead</dt><dd>' + esc(data.leadSource || '—') + '</dd></div>' +
        '<div><dt>Ghi chú khách</dt><dd>' + esc(data.customerNotes || '—') + '</dd></div>' +
        '<div><dt>Loại</dt><dd>' + esc(data.kindLabel || data.kind) + '</dd></div>' +
        '<div><dt>Trạng thái</dt><dd>' + esc(data.statusLabel || STATUS[data.status] || data.status) + '</dd></div>' +
        '</dl>' +
        '<div class="bc-history">' +
        '<h3>Nhật ký trạng thái</h3>' +
        '<ul>' +
        '<li>Tiếp nhận: ' + esc(created) + '</li>' +
        '<li>Hiện tại: ' + esc(data.statusLabel || STATUS[data.status] || '—') +
          (data.isLate ? ' (trễ giờ)' : '') + '</li>' +
        '</ul></div>';
    }

    function openFromCard(card) {
      open();
      var kind = card.getAttribute('data-kind') || '';
      var id = card.getAttribute('data-id') || '';
      if (body) body.innerHTML = '<p class="bc-muted">Đang tải…</p>';
      if (form) form.hidden = true;

      fetch(
        '/admin/bookings?handler=Detail&id=' + encodeURIComponent(id) +
          '&kind=' + encodeURIComponent(kind),
        {
          credentials: 'same-origin',
          headers: { Accept: 'application/json' }
        }
      )
        .then(function (r) {
          if (!r.ok) throw new Error('fail');
          return r.json();
        })
        .then(function (data) {
          renderBody(data);
          if (idInput) idInput.value = data.id;
          if (notes) notes.value = data.adminNotes || '';
          fillStatusOptions(data.status);
          if (form) form.hidden = !data.canEditAdminNotes;
        })
        .catch(function () {
          if (body) body.innerHTML = '<p class="bc-muted">Không tải được chi tiết.</p>';
        });
    }

    page.addEventListener('click', function (e) {
      var btn = e.target.closest('[data-bc-open]');
      if (!btn) return;
      var card = btn.closest('[data-bc-card]');
      if (!card) return;
      e.preventDefault();
      openFromCard(card);
    });

    drawer.querySelectorAll('[data-bc-drawer-close]').forEach(function (el) {
      el.addEventListener('click', function (e) {
        e.preventDefault();
        close();
      });
    });

    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && !drawer.hidden) {
        e.preventDefault();
        close();
      }
    });
  }

  window.bootBookingCenter = boot;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.body.addEventListener('htmx:afterSwap', function (e) {
    if (e.detail && e.detail.target && e.detail.target.id === 'main-content') boot();
  });
})();
