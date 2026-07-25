/**
 * Motorcycle Media Manager (Sprint 2.2) — scoped to motorcycle editor only.
 */
(function () {
  'use strict';

  function qs(sel, root) { return (root || document).querySelector(sel); }
  function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

  function showProgress(label) {
    var wrap = qs('[data-upload-progress]');
    var bar = qs('[data-upload-progress-bar]');
    var text = qs('[data-upload-progress-label]');
    if (!wrap) return;
    wrap.hidden = false;
    if (text) text.textContent = label || 'Đang tải…';
    if (bar) {
      bar.style.width = '15%';
      requestAnimationFrame(function () { bar.style.width = '70%'; });
    }
  }

  function hideProgress() {
    var wrap = qs('[data-upload-progress]');
    var bar = qs('[data-upload-progress-bar]');
    if (bar) bar.style.width = '100%';
    setTimeout(function () {
      if (wrap) wrap.hidden = true;
      if (bar) bar.style.width = '0%';
    }, 400);
  }

  function toastFromFlash() {
    qsa('.admin-flash').forEach(function (el) {
      el.classList.add('admin-toast');
      setTimeout(function () { el.classList.add('is-leaving'); }, 4200);
      setTimeout(function () { el.remove(); }, 4800);
    });
  }

  function bindDropzone(zone) {
    var input = qs('[data-zone-input], [data-thumb-input]', zone) || zone.querySelector('input[type=file]');
    if (!input) return;

    function setFiles(files) {
      if (!files || !files.length) return;
      var dt = new DataTransfer();
      var multiple = input.multiple;
      var list = multiple ? Array.from(files) : [files[0]];
      list.forEach(function (f) {
        if (f.type && f.type.indexOf('image/') === 0) dt.items.add(f);
      });
      if (!dt.files.length) return;
      input.files = dt.files;
      input.dispatchEvent(new Event('change', { bubbles: true }));
      previewThumb(input);
    }

    zone.addEventListener('click', function (e) {
      if (e.target.closest('button, a, label')) return;
      input.click();
    });
    zone.addEventListener('dragover', function (e) {
      e.preventDefault();
      zone.classList.add('is-dragover');
    });
    zone.addEventListener('dragleave', function () { zone.classList.remove('is-dragover'); });
    zone.addEventListener('drop', function (e) {
      e.preventDefault();
      zone.classList.remove('is-dragover');
      setFiles(e.dataTransfer.files);
    });

    document.addEventListener('paste', function (e) {
      if (!zone.matches(':focus, :focus-within') && document.activeElement !== zone) {
        if (!zone.classList.contains('mm-dropzone-thumb')) return;
      }
      var items = e.clipboardData && e.clipboardData.items;
      if (!items) return;
      var files = [];
      for (var i = 0; i < items.length; i++) {
        if (items[i].type.indexOf('image/') === 0) {
          var blob = items[i].getAsFile();
          if (blob) files.push(new File([blob], 'paste-' + Date.now() + '.png', { type: blob.type }));
        }
      }
      if (files.length) {
        e.preventDefault();
        setFiles(files);
      }
    });
  }

  function previewThumb(input) {
    var file = input.files && input.files[0];
    var img = qs('[data-thumb-preview]');
    var ph = qs('[data-thumb-placeholder]');
    var zone = input.closest('.mm-dropzone');
    if (!file || !img) return;
    var url = URL.createObjectURL(file);
    img.src = url;
    img.hidden = false;
    if (ph) ph.hidden = true;
    if (zone) zone.classList.add('has-preview');
  }

  function initSortable(container, orderInputSel, saveBtnSel) {
    if (!container) return;
    var dragEl = null;
    qsa('[draggable=true]', container).forEach(function (card) {
      card.addEventListener('dragstart', function () {
        dragEl = card;
        card.classList.add('is-dragging');
      });
      card.addEventListener('dragend', function () {
        card.classList.remove('is-dragging');
        dragEl = null;
        syncOrder();
      });
      card.addEventListener('dragover', function (e) {
        e.preventDefault();
        if (!dragEl || dragEl === card) return;
        var rect = card.getBoundingClientRect();
        var before = (e.clientY - rect.top) < rect.height / 2;
        container.insertBefore(dragEl, before ? card : card.nextSibling);
      });
    });

    function syncOrder() {
      var ids = qsa('[data-id]', container).map(function (el) { return el.getAttribute('data-id'); });
      var input = qs(orderInputSel);
      var btn = qs(saveBtnSel);
      if (input) input.value = ids.join(',');
      if (btn) btn.hidden = false;
    }
  }

  function initReplaceInputs(root) {
    var id = qs('input[name=Id]', root) ? qs('input[name=Id]').value : null;
    var token = qs('input[name=__RequestVerificationToken]')?.value;
    qsa('[data-replace-submit]', root).forEach(function (input) {
      input.addEventListener('change', function () {
        if (!input.files || !input.files.length) return;
        var handler = input.getAttribute('data-handler');
        var field = input.getAttribute('data-field') || 'imageFile';
        var form = document.createElement('form');
        form.method = 'post';
        form.enctype = 'multipart/form-data';
        form.action = window.location.pathname + '?handler=' + handler;
        var t = document.createElement('input');
        t.type = 'hidden'; t.name = '__RequestVerificationToken'; t.value = token;
        form.appendChild(t);
        if (id) {
          var hid = document.createElement('input');
          hid.type = 'hidden'; hid.name = 'Id'; hid.value = id;
          form.appendChild(hid);
        }
        var extraName = input.getAttribute('data-extra-name');
        var extraValue = input.getAttribute('data-extra-value');
        if (extraName) {
          var ex = document.createElement('input');
          ex.type = 'hidden'; ex.name = extraName; ex.value = extraValue;
          form.appendChild(ex);
        }
        var fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.name = field;
        fileInput.files = input.files;
        // DataTransfer copy
        var dt = new DataTransfer();
        dt.items.add(input.files[0]);
        fileInput.files = dt.files;
        form.appendChild(fileInput);
        document.body.appendChild(form);
        showProgress('Đang thay ảnh…');
        form.submit();
      });
    });
  }

  function initBulkDelete() {
    var btn = qs('[data-bulk-delete-btn]');
    if (!btn) return;
    function refresh() {
      var n = qsa('[data-bulk-check]:checked').length;
      btn.disabled = n === 0;
      btn.textContent = n ? ('Bulk delete (' + n + ')') : 'Bulk delete';
    }
    qsa('[data-bulk-check]').forEach(function (c) {
      c.addEventListener('change', refresh);
    });
    refresh();
  }

  function initColorPreview() {
    var img = qs('[data-color-preview-img]');
    var name = qs('[data-color-preview-name]');
    qsa('[data-color-select]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var card = btn.closest('[data-color-image]');
        if (!card) return;
        qsa('.mm-color-card').forEach(function (c) { c.classList.remove('is-selected'); });
        card.classList.add('is-selected');
        if (img) img.src = card.getAttribute('data-color-image') || img.src;
        if (name) name.textContent = card.getAttribute('data-color-name') || '';
      });
    });
  }

  function initSpinPreview() {
    var frames = qsa('.mm-spin-frame[data-src]');
    var img = qs('[data-spin-preview-img]');
    var label = qs('[data-spin-frame-label]');
    var play = qs('[data-spin-play]');
    if (!frames.length || !img || !play) return;
    var i = 0;
    var timer = null;
    function show(idx) {
      i = idx % frames.length;
      img.src = frames[i].getAttribute('data-src');
      if (label) label.textContent = 'Frame ' + String(i + 1).padStart(3, '0') + ' / ' + String(frames.length).padStart(3, '0');
    }
    play.addEventListener('click', function () {
      if (timer) {
        clearInterval(timer);
        timer = null;
        play.textContent = '▶ Preview';
        return;
      }
      play.textContent = '❚❚ Stop';
      timer = setInterval(function () { show(i + 1); }, 80);
    });
  }

  function initUploadForms() {
    qsa('[data-upload-form]').forEach(function (form) {
      form.addEventListener('submit', function () {
        showProgress(form.getAttribute('data-upload-label') || 'Đang tải…');
      });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    toastFromFlash();
    var root = qs('[data-motorcycle-media]') || document;
    qsa('[data-dropzone]', root).forEach(bindDropzone);
    qsa('[data-thumb-input]').forEach(function (input) {
      input.addEventListener('change', function () { previewThumb(input); });
    });
    initSortable(qs('[data-sortable=gallery]'), '[data-gallery-order]', '[data-gallery-save-order]');
    initSortable(qs('[data-sortable=colors]'), '[data-color-order]', '[data-color-save-order]');
    initSortable(qs('[data-sortable=spin]'), '[data-spin-order]', '[data-spin-save-order]');
    initReplaceInputs(root);
    initBulkDelete();
    initColorPreview();
    initSpinPreview();
    initUploadForms();
    // Quick create dropzone outside media root
    qsa('[data-dropzone]').forEach(bindDropzone);
  });

  window.addEventListener('pageshow', hideProgress);
})();
