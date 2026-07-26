/**
 * Motorcycle Media Studio — visual async CMS (Shopify-like).
 * One module. Talks only to /admin/api/xe/{id}/media.
 */
(function () {
  'use strict';

  function qs(sel, root) { return (root || document).querySelector(sel); }
  function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

  function formatBytes(n) {
    if (!n) return '—';
    if (n < 1024) return n + ' B';
    if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB';
    return (n / (1024 * 1024)).toFixed(2) + ' MB';
  }

  function esc(s) {
    return String(s || '').replace(/[&<>"']/g, (c) => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[c]);
  }

  class MediaStudio {
    constructor(root) {
      this.root = root;
      this.api = root.dataset.api;
      this.state = null;
      this.selectedGallery = new Set();
      this.busy = false;
      this.spinTimer = null;
      this.spinIndex = 0;
    }

    async init() {
      await this.reload();
      this.bindGlobal();
    }

    async reload() {
      this.root.innerHTML = '<div class="ms-loading">Đang tải Media Studio…</div>';
      try {
        const res = await fetch(this.api, { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Không tải được media.');
        this.state = await res.json();
        this.render();
      } catch (err) {
        this.root.innerHTML = '<div class="ms-error">' + esc(err.message || err) + '</div>';
      }
    }

    async mutate(path, options) {
      if (this.busy) return;
      this.busy = true;
      this.showProgress(true, options && options.label);
      try {
        const res = await fetch(this.api + path, {
          method: options.method || 'POST',
          credentials: 'same-origin',
          body: options.body,
          headers: options.headers || undefined
        });
        const data = await res.json();
        if (data.state) this.state = data.state;
        else if (data.State) this.state = data.State;
        if (data.success === false || data.Success === false) {
          this.toast(data.message || data.Message || 'Thất bại', true);
        } else if (data.message || data.Message) {
          this.toast(data.message || data.Message, false);
        }
        this.render();
        return data;
      } catch (err) {
        this.toast(err.message || 'Lỗi mạng', true);
      } finally {
        this.busy = false;
        this.showProgress(false);
      }
    }

    toast(msg, isError) {
      let el = qs('[data-ms-toast]', this.root);
      if (!el) {
        el = document.createElement('div');
        el.className = 'ms-toast';
        el.setAttribute('data-ms-toast', '');
        this.root.appendChild(el);
      }
      el.textContent = msg;
      el.classList.toggle('is-error', !!isError);
      el.hidden = false;
      clearTimeout(this._toastT);
      this._toastT = setTimeout(() => { el.hidden = true; }, 3200);
    }

    showProgress(on, label) {
      const bar = qs('[data-ms-progress]', this.root);
      if (!bar) return;
      bar.hidden = !on;
      const lab = qs('[data-ms-progress-label]', this.root);
      if (lab) lab.textContent = label || 'Đang tải…';
    }

    render() {
      const s = this.state;
      if (!s) return;
      const health = s.health || s.Health;
      const publish = s.publish || s.Publish;
      const gallery = s.gallery || s.Gallery || [];
      const colors = s.colors || s.Colors || [];
      const spin = s.spin || s.Spin;
      const thumb = s.thumbnail || s.Thumbnail;
      const hero = s.hero || s.Hero;

      this.root.innerHTML = `
        <div class="ms-progress" data-ms-progress hidden>
          <div class="ms-progress-bar"></div>
          <p data-ms-progress-label>Đang tải…</p>
        </div>
        <div class="ms-toast" data-ms-toast hidden></div>

        <header class="ms-header">
          <div>
            <h2 class="ms-title">Media Studio</h2>
            <p class="ms-sub">${esc(s.name || s.Name)} · kéo thả · dán Ctrl+V · không cần reload</p>
          </div>
          <div class="ms-header-meta">
            <span class="ms-pill ${s.supportsUpload || s.SupportsUpload ? 'is-ok' : 'is-off'}">${esc(s.storageNote || s.StorageNote || '')}</span>
          </div>
        </header>

        <section class="ms-dashboard">
          <div class="ms-score">
            <div class="ms-score-ring" style="--p:${(health && (health.scorePercent ?? health.ScorePercent)) || 0}">
              <strong>${(health && (health.scorePercent ?? health.ScorePercent)) || 0}%</strong>
              <span>Media Health</span>
            </div>
          </div>
          <div class="ms-health-list">
            ${(health && (health.items || health.Items) || []).map(it => `
              <div class="ms-health-item is-${esc(it.status || it.Status)}">
                <span class="ms-health-dot"></span>
                <div>
                  <strong>${esc(it.label || it.Label)}</strong>
                  <p>${esc(it.detail || it.Detail)}</p>
                </div>
              </div>`).join('')}
          </div>
          <div class="ms-publish ${publish && (publish.ready ?? publish.Ready) ? 'is-ready' : 'is-warn'}">
            <strong>${esc(publish && (publish.statusLabel || publish.StatusLabel))}</strong>
            <ul>${(publish && (publish.missing || publish.Missing) || []).map(m => `<li>${esc(m)}</li>`).join('') || '<li>Tất cả sẵn sàng</li>'}</ul>
          </div>
        </section>

        <div class="ms-grid-top">
          ${this.renderSlot('Thumbnail', 'thumbnail', thumb, '1200×1200 khuyến nghị · danh sách & SEO')}
          ${this.renderSlot('Hero Image', 'hero', hero, '1920×1080 khuyến nghị · Desktop / Mobile')}
        </div>

        ${this.renderImport()}
        ${this.renderGallery(gallery)}
        ${this.renderColors(colors)}
        ${this.renderSpin(spin)}
      `;

      this.bindAfterRender();
    }

    renderSlot(title, slot, data, hint) {
      const url = data && (data.url || data.Url);
      return `
        <section class="ms-card ms-slot" data-slot="${slot}">
          <div class="ms-card-head">
            <h3>${esc(title)}</h3>
            <p class="ms-hint">${esc(hint)}</p>
          </div>
          <div class="ms-dropzone ${url ? 'has-image' : ''}" data-drop="${slot}" tabindex="0">
            ${url
              ? `<img src="${esc(url)}" alt="${esc(title)}" class="ms-slot-img" />`
              : `<div class="ms-drop-empty"><strong>Kéo ảnh vào đây</strong><span>hoặc bấm · Ctrl+V</span></div>`}
            <input type="file" accept="image/*" hidden data-file="${slot}" />
          </div>
          <div class="ms-slot-meta">
            <span>${url ? formatBytes(data.bytes || data.Bytes) : 'Chưa có ảnh'}</span>
            <span>Crop · sắp có</span>
          </div>
          <div class="ms-actions">
            <button type="button" class="ms-btn primary" data-pick="${slot}">${url ? 'Replace' : 'Upload'}</button>
            ${url ? `<button type="button" class="ms-btn danger" data-clear="${slot}">Remove</button>` : ''}
          </div>
        </section>`;
    }

    renderImport() {
      return `
        <section class="ms-card ms-import">
          <div class="ms-card-head">
            <h3>Smart Import</h3>
            <p class="ms-hint">Chọn cả thư mục xe: thumbnail.jpg · hero.jpg · gallery/ · colors/ · 360/</p>
          </div>
          <div class="ms-dropzone ms-dropzone-wide" data-drop="import" tabindex="0">
            <div class="ms-drop-empty"><strong>Kéo thư mục hoặc chọn nhiều file</strong><span>Hệ thống tự nhận diện</span></div>
            <input type="file" multiple webkitdirectory directory hidden data-file="import" />
            <input type="file" multiple accept="image/*" hidden data-file="import-files" />
          </div>
          <div class="ms-actions">
            <button type="button" class="ms-btn primary" data-pick="import">Chọn thư mục</button>
            <button type="button" class="ms-btn" data-pick="import-files">Chọn nhiều ảnh</button>
          </div>
          <div class="ms-import-summary" data-import-summary hidden></div>
        </section>`;
    }

    renderGallery(gallery) {
      return `
        <section class="ms-card">
          <div class="ms-card-head row">
            <div>
              <h3>Gallery <span class="ms-count">${gallery.length}</span></h3>
              <p class="ms-hint">Multi-upload · kéo sắp xếp · auto-save · bulk delete</p>
            </div>
            <div class="ms-actions">
              <button type="button" class="ms-btn danger" data-gallery-bulk disabled>Xóa đã chọn</button>
              <button type="button" class="ms-btn primary" data-pick="gallery">Thêm ảnh</button>
              <input type="file" accept="image/*" multiple hidden data-file="gallery" />
            </div>
          </div>
          <div class="ms-dropzone ms-dropzone-wide ${gallery.length ? 'is-compact' : ''}" data-drop="gallery" tabindex="0">
            ${gallery.length === 0
              ? `<div class="ms-drop-empty"><strong>Kéo nhiều ảnh gallery</strong></div>`
              : `<div class="ms-gallery-grid" data-sortable="gallery">
                  ${gallery.map(g => {
                    const id = g.id || g.Id;
                    const url = g.url || g.Url;
                    const alt = g.altText || g.AltText || '';
                    const sel = this.selectedGallery.has(id);
                    return `<article class="ms-g-card ${sel ? 'is-selected' : ''}" draggable="true" data-id="${id}">
                      <label class="ms-check"><input type="checkbox" data-g-check value="${id}" ${sel ? 'checked' : ''}/></label>
                      <img src="${esc(url)}" alt="" />
                      <input type="text" class="ms-caption" data-caption="${id}" value="${esc(alt)}" placeholder="Chú thích (alt)" />
                    </article>`;
                  }).join('')}
                </div>`}
          </div>
        </section>`;
    }

    renderColors(colors) {
      return `
        <section class="ms-card">
          <div class="ms-card-head row">
            <div>
              <h3>Colors <span class="ms-count">${colors.length}</span></h3>
              <p class="ms-hint">Mỗi màu là một thẻ trực quan — bấm để quản lý</p>
            </div>
            <button type="button" class="ms-btn primary" data-color-add>+ Thêm màu</button>
          </div>
          <div class="ms-color-grid" data-sortable="colors">
            ${colors.map(c => {
              const id = c.id || c.Id;
              const img = c.imageUrl || c.ImageUrl;
              return `<article class="ms-color-card" draggable="true" data-color-id="${id}">
                <div class="ms-color-swatch" style="background:${esc(c.hexCode || c.HexCode)}"></div>
                <img src="${esc(img || '/images/motorcycles/default.svg')}" alt="" />
                <div class="ms-color-body">
                  <strong>${esc(c.name || c.Name)}</strong>
                  <span>${esc(c.hexCode || c.HexCode)}</span>
                  <span class="ms-mini">Gallery ${(c.galleryCount ?? c.GalleryCount) || 0} · 360 ${(c.spinCount ?? c.SpinCount) || 0}</span>
                </div>
              </article>`;
            }).join('') || '<p class="ms-hint">Chưa có màu — thêm màu đầu tiên.</p>'}
          </div>
          <dialog class="ms-dialog" data-color-dialog>
            <form method="dialog" class="ms-dialog-form" data-color-form>
              <h3 data-color-dialog-title>Màu sắc</h3>
              <input type="hidden" name="colorId" />
              <label>Tên<input name="name" required placeholder="Đen bóng" /></label>
              <label>Hex<input name="hex" required placeholder="#111111" /></label>
              <label>Ảnh đại diện<input type="file" name="image" accept="image/*" /></label>
              <div class="ms-actions">
                <button type="submit" class="ms-btn primary">Lưu</button>
                <button type="button" class="ms-btn danger" data-color-delete hidden>Xóa màu</button>
                <button type="button" class="ms-btn" data-color-close>Đóng</button>
              </div>
            </form>
          </dialog>
        </section>`;
    }

    renderSpin(spin) {
      const frames = (spin && (spin.frames || spin.Frames)) || [];
      const label = spin && (spin.statusLabel || spin.StatusLabel);
      const missing = (spin && (spin.missingIndices || spin.MissingIndices)) || [];
      const first = frames[0] && (frames[0].url || frames[0].Url);
      return `
        <section class="ms-card">
          <div class="ms-card-head row">
            <div>
              <h3>360 Studio</h3>
              <p class="ms-hint">${esc(label)} ${missing.length ? '· thiếu ' + missing.slice(0, 8).map(i => String(i + 1).padStart(3, '0')).join(', ') : ''}</p>
            </div>
            <div class="ms-actions">
              <button type="button" class="ms-btn danger" data-spin-clear ${frames.length ? '' : 'hidden'}>Xóa tất cả</button>
              <button type="button" class="ms-btn primary" data-pick="spin">Tải khung</button>
              <input type="file" accept="image/*" multiple hidden data-file="spin" />
            </div>
          </div>
          <div class="ms-spin-layout">
            <div class="ms-spin-preview" data-spin-preview>
              ${first ? `<img src="${esc(first)}" alt="360" data-spin-img />` : '<div class="ms-drop-empty">Kéo 001.jpg … 036.jpg</div>'}
              <div class="ms-spin-controls" ${frames.length < 2 ? 'hidden' : ''}>
                <button type="button" data-spin-play>Play</button>
                <input type="range" min="0" max="${Math.max(frames.length - 1, 0)}" value="0" data-spin-scrub />
              </div>
            </div>
            <div class="ms-dropzone ms-dropzone-wide" data-drop="spin" tabindex="0">
              <div class="ms-spin-strip" data-sortable="spin">
                ${frames.map(f => `<button type="button" class="ms-spin-thumb" draggable="true" data-id="${f.id || f.Id}" data-spin-goto="${f.frameIndex ?? f.FrameIndex}">
                  <img src="${esc(f.url || f.Url)}" alt="" /><span>${esc(f.label || f.Label)}</span>
                </button>`).join('') || '<div class="ms-drop-empty"><strong>Thả khung 360 tại đây</strong></div>'}
              </div>
            </div>
          </div>
        </section>`;
    }

    bindGlobal() {
      document.addEventListener('paste', (e) => {
        if (!this.root.isConnected) return;
        const items = e.clipboardData && e.clipboardData.items;
        if (!items) return;
        for (const item of items) {
          if (item.type.indexOf('image') === 0) {
            const file = item.getAsFile();
            if (file) this.uploadSlot('thumbnail', file);
            break;
          }
        }
      });
    }

    bindAfterRender() {
      qsa('[data-pick]', this.root).forEach(btn => {
        btn.addEventListener('click', () => {
          const key = btn.getAttribute('data-pick');
          const input = qs(`[data-file="${key}"]`, this.root);
          if (input) input.click();
        });
      });

      qsa('[data-file]', this.root).forEach(input => {
        input.addEventListener('change', () => {
          const key = input.getAttribute('data-file');
          const files = Array.from(input.files || []);
          input.value = '';
          if (!files.length) return;
          if (key === 'thumbnail' || key === 'hero') this.uploadSlot(key, files[0]);
          else if (key === 'gallery') this.uploadGallery(files);
          else if (key === 'spin') this.uploadSpin(files);
          else if (key === 'import' || key === 'import-files') this.smartImport(files);
        });
      });

      qsa('[data-drop]', this.root).forEach(zone => {
        const slot = zone.getAttribute('data-drop');
        zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('is-drag'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('is-drag'));
        zone.addEventListener('drop', (e) => {
          e.preventDefault();
          zone.classList.remove('is-drag');
          const files = Array.from(e.dataTransfer.files || []);
          if (!files.length) return;
          if (slot === 'thumbnail' || slot === 'hero') this.uploadSlot(slot, files[0]);
          else if (slot === 'gallery') this.uploadGallery(files);
          else if (slot === 'spin') this.uploadSpin(files);
          else if (slot === 'import') this.smartImport(files);
        });
        zone.addEventListener('click', (e) => {
          if (e.target.closest('input,button,a,article,.ms-g-card,.ms-spin-thumb')) return;
          const input = qs(`[data-file="${slot}"]`, this.root) || qs(`[data-file="${slot}-files"]`, this.root);
          if (input) input.click();
        });
      });

      qsa('[data-clear]', this.root).forEach(btn => {
        btn.addEventListener('click', () => this.clearSlot(btn.getAttribute('data-clear')));
      });

      this.bindGallery();
      this.bindColors();
      this.bindSpin();
      this.bindSortable();
    }

    bindGallery() {
      qsa('[data-g-check]', this.root).forEach(cb => {
        cb.addEventListener('change', () => {
          if (cb.checked) this.selectedGallery.add(cb.value);
          else this.selectedGallery.delete(cb.value);
          const bulk = qs('[data-gallery-bulk]', this.root);
          if (bulk) bulk.disabled = this.selectedGallery.size === 0;
        });
      });
      const bulk = qs('[data-gallery-bulk]', this.root);
      if (bulk) {
        bulk.addEventListener('click', async () => {
          const ids = Array.from(this.selectedGallery);
          if (!ids.length) return;
          this.selectedGallery.clear();
          await this.mutate('/gallery/delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids }),
            label: 'Đang xóa…'
          });
        });
      }
      qsa('[data-caption]', this.root).forEach(input => {
        let t;
        input.addEventListener('change', () => {
          clearTimeout(t);
          t = setTimeout(() => {
            this.mutate('/gallery/' + input.getAttribute('data-caption') + '/caption', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ caption: input.value })
            });
          }, 400);
        });
      });
    }

    bindColors() {
      const dialog = qs('[data-color-dialog]', this.root);
      const form = qs('[data-color-form]', this.root);
      qs('[data-color-add]', this.root)?.addEventListener('click', () => {
        form.reset();
        form.colorId.value = '';
        qs('[data-color-delete]', form).hidden = true;
        qs('[data-color-dialog-title]', form).textContent = 'Thêm màu';
        dialog.showModal();
      });
      qsa('[data-color-id]', this.root).forEach(card => {
        card.addEventListener('click', () => {
          const id = card.getAttribute('data-color-id');
          const c = (this.state.colors || this.state.Colors || []).find(x => (x.id || x.Id) === id);
          if (!c) return;
          form.colorId.value = id;
          form.name.value = c.name || c.Name;
          form.hex.value = c.hexCode || c.HexCode;
          qs('[data-color-delete]', form).hidden = false;
          qs('[data-color-dialog-title]', form).textContent = 'Quản lý màu';
          dialog.showModal();
        });
      });
      qs('[data-color-close]', this.root)?.addEventListener('click', () => dialog.close());
      qs('[data-color-delete]', this.root)?.addEventListener('click', async () => {
        const id = form.colorId.value;
        if (!id || !confirm('Xóa màu này?')) return;
        dialog.close();
        await this.mutate('/colors/' + id, { method: 'DELETE', label: 'Đang xóa màu…' });
      });
      form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData();
        if (form.colorId.value) fd.append('colorId', form.colorId.value);
        fd.append('name', form.name.value);
        fd.append('hex', form.hex.value);
        if (form.image.files[0]) fd.append('image', form.image.files[0]);
        dialog.close();
        await this.mutate('/colors', { method: 'POST', body: fd, label: 'Đang lưu màu…' });
      });
    }

    bindSpin() {
      const frames = (this.state.spin || this.state.Spin || {}).frames || (this.state.spin || this.state.Spin || {}).Frames || [];
      const img = qs('[data-spin-img]', this.root);
      const scrub = qs('[data-spin-scrub]', this.root);
      const play = qs('[data-spin-play]', this.root);

      const show = (i) => {
        this.spinIndex = i;
        const f = frames[i];
        if (img && f) img.src = f.url || f.Url;
        if (scrub) scrub.value = String(i);
      };

      scrub?.addEventListener('input', () => show(Number(scrub.value)));
      play?.addEventListener('click', () => {
        if (this.spinTimer) {
          clearInterval(this.spinTimer);
          this.spinTimer = null;
          play.textContent = 'Play';
          return;
        }
        play.textContent = 'Pause';
        this.spinTimer = setInterval(() => show((this.spinIndex + 1) % frames.length), 80);
      });
      qsa('[data-spin-goto]', this.root).forEach(btn => {
        btn.addEventListener('click', () => {
          const idx = frames.findIndex(f => String(f.frameIndex ?? f.FrameIndex) === btn.getAttribute('data-spin-goto'));
          if (idx >= 0) show(idx);
        });
      });
      qs('[data-spin-clear]', this.root)?.addEventListener('click', async () => {
        if (!confirm('Xóa toàn bộ khung 360?')) return;
        await this.mutate('/spin', { method: 'DELETE', label: 'Đang xóa 360…' });
      });
    }

    bindSortable() {
      ['gallery', 'colors', 'spin'].forEach(kind => {
        const wrap = qs(`[data-sortable="${kind}"]`, this.root);
        if (!wrap) return;
        let dragEl = null;
        qsa('[draggable]', wrap).forEach(el => {
          el.addEventListener('dragstart', () => { dragEl = el; el.classList.add('is-dragging'); });
          el.addEventListener('dragend', async () => {
            el.classList.remove('is-dragging');
            const ids = qsa('[data-id],[data-color-id]', wrap).map(n => n.getAttribute('data-id') || n.getAttribute('data-color-id'));
            const path = kind === 'gallery' ? '/gallery/reorder' : kind === 'colors' ? '/colors/reorder' : '/spin/reorder';
            await this.mutate(path, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ ids })
            });
          });
          el.addEventListener('dragover', (e) => {
            e.preventDefault();
            if (!dragEl || dragEl === el) return;
            const rect = el.getBoundingClientRect();
            const before = e.clientX < rect.left + rect.width / 2;
            wrap.insertBefore(dragEl, before ? el : el.nextSibling);
          });
        });
      });
    }

    uploadSlot(slot, file) {
      const fd = new FormData();
      fd.append('file', file);
      return this.mutate('/' + slot, { method: 'POST', body: fd, label: 'Đang tải ' + slot + '…' });
    }

    clearSlot(slot) {
      return this.mutate('/' + slot, { method: 'DELETE', label: 'Đang xóa…' });
    }

    uploadGallery(files) {
      const fd = new FormData();
      files.forEach(f => fd.append('files', f));
      return this.mutate('/gallery', { method: 'POST', body: fd, label: 'Đang tải gallery…' });
    }

    uploadSpin(files) {
      const fd = new FormData();
      files.forEach(f => fd.append('files', f));
      return this.mutate('/spin', { method: 'POST', body: fd, label: 'Đang tải 360…' });
    }

    async smartImport(files) {
      const fd = new FormData();
      files.forEach((f, i) => {
        fd.append('files', f, f.name);
        const rel = f.webkitRelativePath || f.name;
        fd.append('paths', rel);
      });
      const data = await this.mutate('/import', { method: 'POST', body: fd, label: 'Đang import thư mục…' });
      const box = qs('[data-import-summary]', this.root);
      if (box && data) {
        box.hidden = false;
        const msg = data.message || data.Message || '';
        const warnings = data.warnings || data.Warnings || [];
        box.innerHTML = `<strong>${esc(msg)}</strong><ul>${warnings.map(w => `<li>${esc(w)}</li>`).join('')}</ul>`;
      }
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    const root = qs('[data-media-studio]');
    if (!root) return;
    new MediaStudio(root).init();
  });
})();
