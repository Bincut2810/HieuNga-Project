/* ─── Shared Image Uploader (Honda Hiếu Nga) ───────────────────────────────
   Used by Promotion, Blog, Bank, Motorcycle media, Banner, Service gallery.

   Single canonical upload endpoint:
       POST /admin/api/upload   (multipart: file, kind, contextId?)

   The widget supports two top-level modes:
     • form-input : write the uploaded URL into a hidden input bound to a form.
                    The surrounding Razor Page POST then saves the URL.
     • domain     : upload to the canonical endpoint, then POST the URL to a
                    configured domain endpoint to persist it.

   Both modes share the same drag/drop, file picker, validation, preview, and
   progress UI. No external dependencies (no jQuery, no CDN scripts).
   ────────────────────────────────────────────────────────────────────────── */

(function () {
  'use strict';

  var CANONICAL_ENDPOINT = '/admin/api/upload';
  var ACCEPT = 'image/jpeg,image/jpg,image/png,image/webp';
  var MAX_BYTES_DEFAULT = 5 * 1024 * 1024;

  /* ─── helpers ─────────────────────────────────────────────── */
  function qs(sel, root) { return (root || document).querySelector(sel); }
  function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }
  /* ─── HTML escape ──────────────────────────────────────────── */
  var ESC_MAP = {
    '&': '&' + 'amp;',
    '<': '&' + 'lt;',
    '>': '&' + 'gt;',
    '"': '&' + 'quot;',
    "'": '&' + '#39;'
  };
  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return ESC_MAP[c]; });
  }
  function getCookie(name) {
    var prefix = name + '=';
    var parts = (document.cookie || '').split(';');
    for (var i = 0; i < parts.length; i++) {
      var c = parts[i].trim();
      if (c.indexOf(prefix) === 0) return decodeURIComponent(c.substring(prefix.length));
    }
    return '';
  }

  /* ─── class ───────────────────────────────────────────────── */
  function ImageUploader(root, options) {
    this.root = root;
    this.opts = options || {};
    this._busy = false;
  }

  ImageUploader.prototype.init = function () {
    var o = this.opts;
    this._renderShell();
    this._bind();
    if (o.initialUrl) this._renderPreview(o.initialUrl);
  };

  ImageUploader.prototype._renderShell = function () {
    var o = this.opts;
    var label = o.label || 'Ảnh';
    var hint = o.hint || 'Kéo ảnh vào hoặc bấm để chọn';
    var accept = o.accept || ACCEPT;

    this.root.classList.add('iu-root');
    this.root.innerHTML =
      '<div class="iu-progress" hidden>' +
        '<div class="iu-progress-bar"></div>' +
        '<p class="iu-progress-label">Đang tải ảnh…</p>' +
      '</div>' +
      '<div class="iu-toast" hidden role="status" aria-live="polite"></div>' +
      '<label class="iu-dropzone" tabindex="0">' +
        '<div class="iu-dropzone-empty">' +
          '<strong>' + esc(label) + '</strong>' +
          '<span>' + esc(hint) + '</span>' +
        '</div>' +
        '<input type="file" class="iu-file" accept="' + esc(accept) + '"' +
          (o.multiple ? ' multiple' : '') + ' hidden />' +
      '</label>' +
      '<div class="iu-actions">' +
        '<button type="button" class="iu-btn iu-btn-primary iu-pick">Chọn ảnh</button>' +
        '<button type="button" class="iu-btn iu-btn-danger iu-clear" hidden>Xóa</button>' +
      '</div>';
  };

  ImageUploader.prototype._bind = function () {
    var self = this;
    var drop = qs('.iu-dropzone', this.root);
    var fileInput = qs('.iu-file', this.root);
    var pickBtn = qs('.iu-pick', this.root);
    var clearBtn = qs('.iu-clear', this.root);

    pickBtn.addEventListener('click', function (e) { e.preventDefault(); fileInput.click(); });

    // Click anywhere on the dropzone (except the hidden file input itself)
    // opens the native picker.
    drop.addEventListener('click', function (e) {
      if (e.target.closest('button,input,label.iu-pick')) return;
      e.preventDefault();
      fileInput.click();
    });
    drop.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        fileInput.click();
      }
    });

    fileInput.addEventListener('change', function () {
      var files = Array.prototype.slice.call(fileInput.files || []);
      fileInput.value = '';
      if (files.length === 0) return;
      if (self.opts.multiple) {
        self._uploadMany(files);
      } else {
        self._uploadOne(files[0]);
      }
    });

    // Drag/drop — bind at the dropzone level so nested elements don't swallow the drop.
    ['dragenter', 'dragover'].forEach(function (evt) {
      drop.addEventListener(evt, function (e) {
        e.preventDefault();
        e.stopPropagation();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
        drop.classList.add('is-drag');
      });
    });
    drop.addEventListener('dragleave', function (e) {
      e.preventDefault();
      e.stopPropagation();
      drop.classList.remove('is-drag');
    });
    drop.addEventListener('drop', function (e) {
      e.preventDefault();
      e.stopPropagation();
      drop.classList.remove('is-drag');
      var dt = e.dataTransfer;
      if (!dt || !dt.files || dt.files.length === 0) return;
      var files = Array.prototype.slice.call(dt.files);
      if (self.opts.multiple) {
        self._uploadMany(files);
      } else {
        self._uploadOne(files[0]);
      }
    });

    if (clearBtn) {
      clearBtn.addEventListener('click', function (e) {
        e.preventDefault();
        if (!window.confirm('Xóa ảnh này?')) return;
        self._clear();
      });
    }
  };

  ImageUploader.prototype._renderPreview = function (url) {
    var drop = qs('.iu-dropzone', this.root);
    var clearBtn = qs('.iu-clear', this.root);
    if (!url) {
      drop.classList.remove('has-image');
      drop.innerHTML =
        '<div class="iu-dropzone-empty">' +
          '<strong>' + esc(this.opts.label || 'Ảnh') + '</strong>' +
          '<span>' + esc(this.opts.hint || 'Kéo ảnh vào hoặc bấm để chọn') + '</span>' +
        '</div>' +
        '<input type="file" class="iu-file" accept="' + esc(this.opts.accept || ACCEPT) + '"' +
          (this.opts.multiple ? ' multiple' : '') + ' hidden />';
      // Re-bind the new file input
      var newInput = qs('.iu-file', this.root);
      var self = this;
      newInput.addEventListener('change', function () {
        var files = Array.prototype.slice.call(newInput.files || []);
        newInput.value = '';
        if (files.length === 0) return;
        if (self.opts.multiple) self._uploadMany(files);
        else self._uploadOne(files[0]);
      });
      if (clearBtn) clearBtn.hidden = true;
      return;
    }
    drop.classList.add('has-image');
    drop.innerHTML =
      '<img class="iu-preview" src="' + esc(url) + '" alt="" />' +
      '<input type="file" class="iu-file" accept="' + esc(this.opts.accept || ACCEPT) + '"' +
        (this.opts.multiple ? ' multiple' : '') + ' hidden />';
    var input2 = qs('.iu-file', this.root);
    var self2 = this;
    input2.addEventListener('change', function () {
      var files = Array.prototype.slice.call(input2.files || []);
      input2.value = '';
      if (files.length === 0) return;
      if (self2.opts.multiple) self2._uploadMany(files);
      else self2._uploadOne(files[0]);
    });
    if (clearBtn) clearBtn.hidden = false;
  };

  ImageUploader.prototype._setProgress = function (on, label) {
    var bar = qs('.iu-progress', this.root);
    if (!bar) return;
    bar.hidden = !on;
    var lab = qs('.iu-progress-label', bar);
    if (lab) lab.textContent = label || 'Đang tải ảnh…';
  };

  ImageUploader.prototype._toast = function (msg, isError) {
    var el = qs('.iu-toast', this.root);
    if (!el) return;
    el.textContent = msg;
    el.classList.toggle('is-error', !!isError);
    el.hidden = false;
    clearTimeout(el._iuTimer);
    el._iuTimer = setTimeout(function () { el.hidden = true; }, 4500);
  };

  ImageUploader.prototype._validate = function (file) {
    if (!file) return 'Chưa chọn ảnh.';
    if (file.size <= 0) return 'Ảnh rỗng.';
    var max = this.opts.maxBytes || MAX_BYTES_DEFAULT;
    if (file.size > max) return 'Ảnh vượt quá ' + Math.round(max / 1024 / 1024) + ' MB.';
    var okExt = /\.(jpe?g|png|webp)$/i.test(file.name || '');
    if (!okExt) return 'Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.';
    return null;
  };

  ImageUploader.prototype._uploadOne = function (file) {
    var self = this;
    var msg = this._validate(file);
    if (msg) { this._toast(msg, true); return; }

    this._setProgress(true, 'Đang tải ảnh lên máy chủ…');
    this._busy = true;

    var fd = new FormData();
    fd.append('file', file);
    if (this.opts.kind) fd.append('kind', this.opts.kind);
    if (this.opts.contextId) fd.append('contextId', this.opts.contextId);

    fetch(CANONICAL_ENDPOINT, {
      method: 'POST',
      credentials: 'same-origin',
      body: fd
    })
      .then(function (res) {
        return res.json().then(function (data) { return { status: res.status, data: data }; });
      })
      .then(function (r) { return self._handleResponse(r.data, r.status, file); })
      .catch(function (err) {
        self._setProgress(false);
        self._busy = false;
        self._toast('Lỗi mạng: ' + (err && err.message ? err.message : err), true);
      });
  };

  ImageUploader.prototype._uploadMany = function (files) {
    var self = this;
    var valid = [];
    for (var i = 0; i < files.length; i++) {
      var msg = this._validate(files[i]);
      if (msg) this._toast(files[i].name + ': ' + msg, true);
      else valid.push(files[i]);
    }
    if (valid.length === 0) return;

    this._setProgress(true, 'Đang tải ' + valid.length + ' ảnh…');
    this._busy = true;

    var uploaded = [];
    var failed = [];
    var remaining = valid.length;
    var done = false;

    function finish() {
      if (done) return;
      done = true;
      self._setProgress(false);
      self._busy = false;
      if (uploaded.length > 0 && self.opts.onMany) self.opts.onMany(uploaded);
      if (uploaded.length > 0 && !self.opts.onMany && self.opts.targetInput) {
        // form-input multi: append URLs to the hidden input as comma list
        self._appendUrlsToInput(uploaded);
      }
      if (uploaded.length === valid.length) {
        self._toast('Đã tải ' + uploaded.length + ' ảnh.');
      } else if (uploaded.length > 0) {
        self._toast('Đã tải ' + uploaded.length + ' / ' + valid.length + ' ảnh.', true);
      } else {
        self._toast('Không tải được ảnh nào.', true);
      }
      if (failed.length > 0) console.warn('[iu] failed uploads:', failed);
    }

    valid.forEach(function (file) {
      var fd = new FormData();
      fd.append('file', file);
      if (self.opts.kind) fd.append('kind', self.opts.kind);
      if (self.opts.contextId) fd.append('contextId', self.opts.contextId);
      fetch(CANONICAL_ENDPOINT, {
        method: 'POST',
        credentials: 'same-origin',
        body: fd
      })
        .then(function (res) {
          return res.json().then(function (data) { return { status: res.status, data: data }; });
        })
        .then(function (r) {
          if (r.data && r.data.ok && r.data.url) {
            uploaded.push(r.data.url);
          } else {
            failed.push({ file: file.name, message: (r.data && (r.data.message || r.data.code)) || ('HTTP ' + r.status) });
          }
        })
        .catch(function (err) {
          failed.push({ file: file.name, message: (err && err.message) || 'network error' });
        })
        .then(function () {
          remaining--;
          if (remaining === 0) finish();
        });
    });
  };

  ImageUploader.prototype._handleResponse = function (data, status, file) {
    this._setProgress(false);
    this._busy = false;
    if (data && data.ok && data.url) {
      this._renderPreview(data.url);
      this._toast('Đã tải ảnh.');
      if (this.opts.onUploaded) this.opts.onUploaded(data.url);
      if (this.opts.targetInput) this._writeUrlToInput(data.url);
      return;
    }
    var message = (data && (data.message || data.code)) || ('Upload thất bại (HTTP ' + status + ').');
    this._toast(message, true);
  };

  ImageUploader.prototype._writeUrlToInput = function (url) {
    var sel = this.opts.targetInput;
    if (!sel) return;
    var input = typeof sel === 'string'
      ? document.querySelector('input[name="' + sel + '"], #' + sel.replace(/\./g, '_'))
      : sel;
    if (!input) {
      console.warn('[iu] target input not found:', sel);
      return;
    }
    input.value = url;
    input.dispatchEvent(new Event('change', { bubbles: true }));
  };

  ImageUploader.prototype._appendUrlsToInput = function (urls) {
    var sel = this.opts.targetInput;
    if (!sel) return;
    var input = typeof sel === 'string'
      ? document.querySelector('input[name="' + sel + '"], #' + sel.replace(/\./g, '_'))
      : sel;
    if (!input) return;
    var existing = (input.value || '').split(',').map(function (s) { return s.trim(); }).filter(Boolean);
    var combined = existing.concat(urls);
    input.value = combined.join(',');
    input.dispatchEvent(new Event('change', { bubbles: true }));
  };

  ImageUploader.prototype._clear = function () {
    if (this.opts.targetInput) this._writeUrlToInput('');
    this._renderPreview(null);
    this._toast('Đã xóa ảnh.');
    if (this.opts.onCleared) this.opts.onCleared();
  };

  /* ─── automatic bootstrap for [data-image-uploader] ──────────── */
  function bootFromDom() {
    qsa('[data-image-uploader]').forEach(function (host) {
      if (host.dataset.iuReady === '1') return;
      host.dataset.iuReady = '1';
      try {
        var opts = {
          mode: host.dataset.mode || 'form-input',
          kind: host.dataset.kind || '',
          contextId: host.dataset.contextId || '',
          endpoint: host.dataset.endpoint || '',
          targetInput: host.dataset.targetInput || '',
          label: host.dataset.label || '',
          hint: host.dataset.hint || '',
          accept: host.dataset.accept || ACCEPT,
          multiple: host.dataset.multiple === 'true',
          initialUrl: host.dataset.initialUrl || ''
        };
        var u = new ImageUploader(host, opts);
        u.init();
        if (opts.mode === 'domain' && opts.endpoint && host.dataset.onUploaded) {
          u.opts.onUploaded = function (url) {
            // domain mode: POST the URL to the configured endpoint
            fetch(opts.endpoint, {
              method: 'POST',
              credentials: 'same-origin',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ url: url })
            }).then(function (res) { return res.json(); })
              .then(function (data) {
                if (data && data.success === false) {
                  u._toast(data.message || 'Không lưu được ảnh.', true);
                }
                if (host.dataset.onSaved) {
                  try { window[host.dataset.onSaved](data); } catch (e) { console.error(e); }
                }
              })
              .catch(function (err) {
                u._toast('Lỗi mạng: ' + (err && err.message ? err.message : err), true);
              });
          };
        }
        host._iu = u;
      } catch (e) {
        console.error('[image-uploader] init failed', e);
      }
    });
  }

  /* ─── exports ──────────────────────────────────────────────── */
  window.HieuNgaUploader = window.HieuNgaUploader || {};
  window.HieuNgaUploader.ImageUploader = ImageUploader;
  window.HieuNgaUploader.CANONICAL_ENDPOINT = CANONICAL_ENDPOINT;
  window.HieuNgaUploader.ACCEPT = ACCEPT;
  window.HieuNgaUploader.MAX_BYTES_DEFAULT = MAX_BYTES_DEFAULT;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootFromDom);
  } else {
    bootFromDom();
  }
})();
