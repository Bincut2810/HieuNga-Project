/**
 * Admin Image Picker — dropzone + click-to-upload + clear.
 *
 * Usage:
 *   <div data-image-picker
 *        data-target="ImageUrl"
 *        data-kind="promotions"
 *        data-label="Ảnh đại diện"
 *        data-hint="Kéo ảnh vào đây — hệ thống tự tải lên">
 *   </div>
 *   <input type="hidden" name="Input.ImageUrl" value="@Model.ImageUrl" />
 *
 * The widget reads/writes the hidden input identified by `data-target`.
 */
(function () {
  'use strict';

  var API = '/admin/api/image-upload';

  function qs(sel, root) { return (root || document).querySelector(sel); }
  function esc(s) {
    return String(s || '').replace(/[&<>"']/g, function (c) {
      return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
    });
  }

  function findTargetInput(root) {
    var name = root.getAttribute('data-target');
    if (!name) return null;
    // Match either by exact name attribute (Input.ImageUrl) or by id (Input_ImageUrl).
    var inputs = document.querySelectorAll('input[type="hidden"], input[type="text"], input[type="url"]');
    for (var i = 0; i < inputs.length; i++) {
      var el = inputs[i];
      if (el.name === name) return el;
      var id = el.id || '';
      if (id === name || id === name.replace(/\./g, '_')) return el;
    }
    return null;
  }

  function renderEmpty(host, label) {
    host.innerHTML =
      '<div class="aip-drop-empty">' +
        '<strong>Kéo ảnh vào đây</strong>' +
        '<span>hoặc bấm để chọn từ máy</span>' +
      '</div>';
  }

  function renderImage(host, url) {
    host.innerHTML =
      '<img class="aip-preview" src="' + esc(url) + '" alt="" />';
    host.classList.add('has-image');
  }

  function setProgress(host, on, label) {
    var bar = qs('.aip-progress', host);
    if (!bar) return;
    bar.hidden = !on;
    var lab = qs('.aip-progress-label', bar);
    if (lab && label) lab.textContent = label;
  }

  function toast(host, msg, isError) {
    var el = qs('.aip-toast', host);
    if (!el) {
      el = document.createElement('div');
      el.className = 'aip-toast';
      host.appendChild(el);
    }
    el.textContent = msg;
    el.classList.toggle('is-error', !!isError);
    el.hidden = false;
    clearTimeout(el._t);
    el._t = setTimeout(function () { el.hidden = true; }, 2800);
  }

  function upload(host, file) {
    var kind = host.getAttribute('data-kind') || 'blog';
    var fd = new FormData();
    fd.append('kind', kind);
    fd.append('file', file);

    var target = findTargetInput(host);
    if (!target) {
      toast(host, 'Không tìm thấy ô lưu URL ảnh.', true);
      return;
    }

    setProgress(host, true, 'Đang tải ảnh lên máy chủ…');
    fetch(API, {
      method: 'POST',
      credentials: 'same-origin',
      body: fd
    })
      .then(function (res) { return res.json(); })
      .then(function (data) {
        setProgress(host, false);
        if (data && data.ok && data.url) {
          target.value = data.url;
          target.dispatchEvent(new Event('change', { bubbles: true }));
          renderImage(host, data.url);
          toast(host, 'Đã tải ảnh lên.');
        } else {
          toast(host, (data && (data.message || data.Message)) || 'Không tải được ảnh.', true);
        }
      })
      .catch(function (err) {
        setProgress(host, false);
        toast(host, 'Lỗi mạng: ' + (err && err.message ? err.message : err), true);
      });
  }

  function clear(host) {
    var target = findTargetInput(host);
    if (target) {
      target.value = '';
      target.dispatchEvent(new Event('change', { bubbles: true }));
    }
    host.classList.remove('has-image');
    renderEmpty(host);
    toast(host, 'Đã xóa ảnh.');
  }

  function init(host) {
    var target = findTargetInput(host);
    var currentUrl = target ? (target.value || '').trim() : '';

    host.classList.add('aip-root');
    host.innerHTML =
      '<div class="aip-progress" hidden>' +
        '<div class="aip-progress-bar"></div>' +
        '<p class="aip-progress-label">Đang tải ảnh…</p>' +
      '</div>' +
      '<div class="aip-toast" hidden></div>' +
      '<div class="aip-dropzone' + (currentUrl ? ' has-image' : '') + '" tabindex="0">' +
        (currentUrl
          ? '<img class="aip-preview" src="' + esc(currentUrl) + '" alt="" />'
          : '<div class="aip-drop-empty">' +
              '<strong>' + esc(host.getAttribute('data-label') || 'Ảnh') + '</strong>' +
              '<span>' + esc(host.getAttribute('data-hint') || 'Kéo ảnh vào hoặc bấm để chọn') + '</span>' +
            '</div>') +
        '<input type="file" accept="image/*" hidden />' +
      '</div>' +
      '<div class="aip-actions">' +
        '<button type="button" class="ms-btn primary">' + (currentUrl ? 'Đổi ảnh' : 'Chọn ảnh') + '</button>' +
        (currentUrl ? '<button type="button" class="ms-btn danger" data-aip-clear>Xóa</button>' : '') +
      '</div>';

    var drop = qs('.aip-dropzone', host);
    var fileInput = qs('input[type="file"]', host);
    var pickBtn = qs('.ms-btn.primary', host);

    pickBtn.addEventListener('click', function (e) {
      e.preventDefault();
      fileInput.click();
    });

    fileInput.addEventListener('change', function () {
      var f = (fileInput.files || [])[0];
      fileInput.value = '';
      if (f) upload(host, f);
    });

    drop.addEventListener('click', function (e) {
      if (e.target.closest('button')) return;
      fileInput.click();
    });

    drop.addEventListener('dragover', function (e) {
      e.preventDefault();
      drop.classList.add('is-drag');
    });
    drop.addEventListener('dragleave', function () {
      drop.classList.remove('is-drag');
    });
    drop.addEventListener('drop', function (e) {
      e.preventDefault();
      drop.classList.remove('is-drag');
      var f = (e.dataTransfer.files || [])[0];
      if (f) upload(host, f);
    });

    var clearBtn = qs('[data-aip-clear]', host);
    if (clearBtn) {
      clearBtn.addEventListener('click', function (e) {
        e.preventDefault();
        if (confirm('Xóa ảnh này?')) clear(host);
      });
    }
  }

  document.addEventListener('DOMContentLoaded', function () {
    var hosts = document.querySelectorAll('[data-image-picker]');
    for (var i = 0; i < hosts.length; i++) {
      try { init(hosts[i]); } catch (e) { console.error('[admin-image-picker] init failed', e); }
    }
  });
})();