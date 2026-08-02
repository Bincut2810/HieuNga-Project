/**
 * Test Ride admin — detail drawer.
 */
(function () {
  'use strict';

  var STATUS = {
    0: 'Chờ xác nhận',
    1: 'Đã xác nhận',
    2: 'Hoàn thành',
    3: 'Đã hủy',
    Pending: 'Chờ xác nhận',
    Confirmed: 'Đã xác nhận',
    Completed: 'Hoàn thành',
    Cancelled: 'Đã hủy'
  };

  function boot() {
    var root = document.querySelector('[data-tr-admin]');
    var drawer = document.querySelector('[data-tr-drawer]');
    if (!root || !drawer || root.dataset.ready === '1') return;
    root.dataset.ready = '1';

    var body = drawer.querySelector('[data-tr-drawer-body]');
    var form = drawer.querySelector('[data-tr-drawer-form]');
    var idInput = drawer.querySelector('[data-tr-drawer-id]');
    var statusSelect = drawer.querySelector('[data-tr-drawer-status]');
    var notes = drawer.querySelector('[data-tr-drawer-notes]');

    function close() {
      drawer.hidden = true;
      document.body.classList.remove('tr-drawer-open');
    }

    function open() {
      drawer.hidden = false;
      document.body.classList.add('tr-drawer-open');
    }

    function statusLabel(s) {
      return STATUS[s] || String(s);
    }

    function fillStatusOptions(current) {
      if (!statusSelect) return;
      var cur = typeof current === 'string' ? current : Number(current);
      var map = [
        { v: 0, n: 'Pending', l: 'Chờ xác nhận' },
        { v: 1, n: 'Confirmed', l: 'Đã xác nhận' },
        { v: 2, n: 'Completed', l: 'Hoàn thành' },
        { v: 3, n: 'Cancelled', l: 'Đã hủy' }
      ];
      var allowed = [cur];
      if (cur === 0 || cur === 'Pending') allowed = [0, 1, 2, 3];
      else if (cur === 1 || cur === 'Confirmed') allowed = [1, 2, 3];
      else if (cur === 2 || cur === 'Completed') allowed = [2];
      else if (cur === 3 || cur === 'Cancelled') allowed = [3];

      statusSelect.innerHTML = '';
      map.forEach(function (opt) {
        if (allowed.indexOf(opt.v) < 0 && allowed.indexOf(opt.n) < 0) return;
        var o = document.createElement('option');
        o.value = String(opt.v);
        o.textContent = opt.l;
        if (opt.v === cur || opt.n === cur) o.selected = true;
        statusSelect.appendChild(o);
      });
    }

    function renderDetail(item) {
      if (!body) return;
      var date = item.appointmentDate
        ? new Date(item.appointmentDate).toLocaleDateString('vi-VN')
        : '—';
      body.innerHTML =
        '<dl class="tr-dl">' +
        '<div><dt>Khách hàng</dt><dd>' + esc(item.customerName) + '</dd></div>' +
        '<div><dt>Điện thoại</dt><dd><a href="tel:' + esc(item.phoneNumber) + '">' + esc(item.phoneNumber) + '</a></dd></div>' +
        '<div><dt>Xe</dt><dd>' + esc(item.motorcycleName) + '</dd></div>' +
        '<div><dt>Ngày</dt><dd>' + esc(date) + '</dd></div>' +
        '<div><dt>Giờ</dt><dd>' + esc(item.appointmentTime || '—') + '</dd></div>' +
        '<div><dt>Ghi chú khách</dt><dd>' + esc(item.customerNotes || '—') + '</dd></div>' +
        '<div><dt>Trạng thái</dt><dd>' + esc(statusLabel(item.status)) + '</dd></div>' +
        '</dl>';
      if (idInput) idInput.value = item.id;
      if (notes) notes.value = item.adminNotes || '';
      fillStatusOptions(item.status);
    }

    function esc(s) {
      return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
      });
    }

    root.addEventListener('click', function (e) {
      var btn = e.target.closest('[data-tr-open]');
      if (!btn) return;
      var id = btn.getAttribute('data-tr-open');
      if (!id) return;
      open();
      if (body) body.innerHTML = '<p class="tr-muted">Đang tải…</p>';
      fetch('/admin/test-ride?handler=Detail&id=' + encodeURIComponent(id), {
        credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      })
        .then(function (r) {
          if (!r.ok) throw new Error('fail');
          return r.json();
        })
        .then(renderDetail)
        .catch(function () {
          if (body) body.innerHTML = '<p class="tr-muted">Không tải được chi tiết.</p>';
        });
    });

    drawer.querySelectorAll('[data-tr-drawer-close]').forEach(function (el) {
      el.addEventListener('click', close);
    });

    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && !drawer.hidden) close();
    });
  }

  window.bootTestRideAdmin = boot;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
