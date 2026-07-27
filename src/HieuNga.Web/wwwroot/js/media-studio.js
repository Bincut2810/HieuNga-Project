/**
 * Hình ảnh xe — simple dealership upload (Ảnh đại diện · Ảnh giới thiệu · 6 góc xe).
 * Drop = upload + save. Drag gallery = auto reorder. No Save for images.
 */
(function () {
  'use strict';

  function qs(sel, root) { return (root || document).querySelector(sel); }
  function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

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
      this._ops = 0;
    }

    async init() {
      await this.reload();
    }

    async reload() {
      this.root.innerHTML = '<div class="ms-loading">Đang tải hình ảnh…</div>';
      try {
        const res = await fetch(this.api, { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Không tải được hình ảnh.');
        this.state = await res.json();
        this.render();
      } catch (err) {
        this.root.innerHTML = '<div class="ms-error">' + esc(err.message || err) + '</div>';
      }
    }

    async mutate(path, options) {
      this._ops += 1;
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
          this.toast(data.message || data.Message || 'Không thành công', true);
        } else if (data.message || data.Message) {
          this.toast(data.message || data.Message, false);
        }
        this.render();
        return data;
      } catch (err) {
        this.toast(err.message || 'Lỗi mạng', true);
      } finally {
        this._ops = Math.max(0, this._ops - 1);
        if (this._ops === 0) this.showProgress(false);
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
      this._toastT = setTimeout(() => { el.hidden = true; }, 2800);
    }

    showProgress(on, label) {
      const bar = qs('[data-ms-progress]', this.root);
      if (!bar) return;
      bar.hidden = !on;
      const lab = qs('[data-ms-progress-label]', this.root);
      if (lab) lab.textContent = label || 'Đang tải ảnh…';
    }

    render() {
      const s = this.state;
      if (!s) return;
      const publish = s.publish || s.Publish;
      const gallery = s.gallery || s.Gallery || [];
      const angles = s.angles || s.Angles;
      const thumb = s.thumbnail || s.Thumbnail;
      const ready = publish && (publish.ready ?? publish.Ready);
      const missing = (publish && (publish.missing || publish.Missing)) || [];

      this.root.innerHTML = `
        <div class="ms-progress" data-ms-progress hidden>
          <div class="ms-progress-bar"></div>
          <p data-ms-progress-label>Đang tải ảnh…</p>
        </div>
        <div class="ms-toast" data-ms-toast hidden></div>

        <header class="ms-header">
          <div>
            <h2 class="ms-title">Hình ảnh xe</h2>
            <p class="ms-sub">Kéo ảnh vào khung — hệ thống tự lưu. Không cần bấm Lưu.</p>
          </div>
        </header>

        <section class="ms-complete ${ready ? 'is-ready' : 'is-warn'}" aria-label="Hoàn thiện hình ảnh">
          <strong>${ready ? '✓ Đủ hình để đăng' : 'Hoàn thiện hình ảnh'}</strong>
          <ul>
            ${missing.length
              ? missing.map(m => `<li>${esc(m)}</li>`).join('')
              : '<li>Ảnh đại diện · Ảnh giới thiệu · 6 góc xe</li>'}
          </ul>
        </section>

        ${this.renderAvatar(thumb)}
        ${this.renderGallery(gallery)}
        ${this.renderAngles(angles)}
      `;

      this.bindAfterRender();
    }

    renderAvatar(data) {
      const url = data && (data.url || data.Url);
      return `
        <section class="ms-card ms-slot" data-slot="thumbnail">
          <div class="ms-card-head">
            <h3>1. Ảnh đại diện</h3>
            <p class="ms-hint">Một ảnh chính — hiện trên danh sách và trang chi tiết</p>
          </div>
          <div class="ms-dropzone ms-dropzone-lg ${url ? 'has-image' : ''}" data-drop="thumbnail" tabindex="0" role="button" aria-label="Thêm ảnh đại diện">
            ${url
              ? `<img src="${esc(url)}" alt="Ảnh đại diện" class="ms-slot-img" />`
              : `<div class="ms-drop-empty"><strong>Kéo ảnh vào đây</strong><span>hoặc chạm để chọn từ máy</span></div>`}
            <input type="file" accept="image/*" hidden data-file="thumbnail" />
          </div>
          <div class="ms-actions">
            <button type="button" class="ms-btn primary ms-btn-lg" data-pick="thumbnail">${url ? 'Đổi ảnh' : 'Chọn ảnh'}</button>
            ${url ? `<button type="button" class="ms-btn danger ms-btn-lg" data-clear="thumbnail">Xóa</button>` : ''}
          </div>
        </section>`;
    }

    renderGallery(gallery) {
      return `
        <section class="ms-card">
          <div class="ms-card-head">
            <h3>2. Ảnh giới thiệu <span class="ms-count">${gallery.length}</span></h3>
            <p class="ms-hint">Kéo nhiều ảnh · tự tải · kéo để đổi thứ tự (tự lưu)</p>
          </div>
          <div class="ms-dropzone ms-dropzone-wide ms-dropzone-lg" data-drop="gallery" tabindex="0" role="button" aria-label="Thêm ảnh giới thiệu">
            <div class="ms-drop-empty">
              <strong>Kéo ảnh vào đây</strong>
              <span>có thể chọn nhiều ảnh cùng lúc</span>
            </div>
            <input type="file" accept="image/*" multiple hidden data-file="gallery" />
          </div>
          ${gallery.length
            ? `<div class="ms-gallery-grid" data-sortable="gallery">
                ${gallery.map(g => {
                  const id = g.id || g.Id;
                  const url = g.url || g.Url;
                  return `<article class="ms-g-card" draggable="true" data-id="${id}">
                    <img src="${esc(url)}" alt="" />
                    <button type="button" class="ms-g-del" data-g-del="${id}" aria-label="Xóa ảnh">×</button>
                  </article>`;
                }).join('')}
              </div>`
            : ''}
        </section>`;
    }

    renderAngles(angles) {
      const slots = (angles && (angles.slots || angles.Slots)) || [];
      const filled = angles && (angles.filledCount ?? angles.FilledCount) || 0;
      return `
        <section class="ms-card">
          <div class="ms-card-head">
            <h3>3. 6 góc xe <span class="ms-count">${filled}/6</span></h3>
            <p class="ms-hint">Mỗi ô một góc — kéo ảnh vào ô tương ứng</p>
          </div>
          <div class="ms-angle-grid">
            ${slots.map(slot => {
              const key = slot.key || slot.Key;
              const url = slot.url || slot.Url;
              const lab = slot.label || slot.Label;
              return `<article class="ms-angle-slot" data-angle-key="${esc(key)}">
                <div class="ms-dropzone ms-angle-drop ${url ? 'has-image' : ''}" data-drop-angle="${esc(key)}" tabindex="0" role="button" aria-label="${esc(lab)}">
                  ${url
                    ? `<img src="${esc(url)}" alt="${esc(lab)}" />`
                    : `<div class="ms-drop-empty"><strong>${esc(lab)}</strong><span>Kéo ảnh vào</span></div>`}
                  <input type="file" accept="image/*" hidden data-file-angle="${esc(key)}" />
                </div>
                <div class="ms-angle-bar">
                  <strong>${esc(lab)}</strong>
                  ${url ? `<button type="button" class="ms-btn danger" data-clear-angle="${esc(key)}">Xóa</button>` : ''}
                </div>
              </article>`;
            }).join('') || '<p class="ms-hint">Không tải được danh sách góc.</p>'}
          </div>
        </section>`;
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
          if (key === 'thumbnail') this.uploadSlot('thumbnail', files[0]);
          else if (key === 'gallery') this.uploadGallery(files);
        });
      });

      qsa('[data-drop]', this.root).forEach(zone => {
        const slot = zone.getAttribute('data-drop');
        zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('is-drag'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('is-drag'));
        zone.addEventListener('drop', (e) => {
          e.preventDefault();
          zone.classList.remove('is-drag');
          const files = Array.from(e.dataTransfer.files || []).filter(f => f.type.indexOf('image') === 0 || /\.(jpe?g|png|webp|gif|svg)$/i.test(f.name));
          if (!files.length) return;
          if (slot === 'thumbnail') this.uploadSlot('thumbnail', files[0]);
          else if (slot === 'gallery') this.uploadGallery(files);
        });
        zone.addEventListener('click', (e) => {
          if (e.target.closest('button,input')) return;
          const input = qs(`[data-file="${slot}"]`, this.root);
          if (input) input.click();
        });
      });

      qsa('[data-clear]', this.root).forEach(btn => {
        btn.addEventListener('click', () => {
          if (!confirm('Xóa ảnh đại diện?')) return;
          this.clearSlot(btn.getAttribute('data-clear'));
        });
      });

      qsa('[data-g-del]', this.root).forEach(btn => {
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const id = btn.getAttribute('data-g-del');
          if (!id || !confirm('Xóa ảnh này?')) return;
          await this.mutate('/gallery/delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids: [id] }),
            label: 'Đang xóa…'
          });
        });
      });

      this.bindAngles();
      this.bindGallerySort();
    }

    bindAngles() {
      qsa('[data-file-angle]', this.root).forEach(input => {
        input.addEventListener('change', () => {
          const key = input.getAttribute('data-file-angle');
          const file = (input.files || [])[0];
          input.value = '';
          if (file) this.uploadAngle(key, file);
        });
      });

      qsa('[data-drop-angle]', this.root).forEach(zone => {
        const key = zone.getAttribute('data-drop-angle');
        zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('is-drag'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('is-drag'));
        zone.addEventListener('drop', (e) => {
          e.preventDefault();
          zone.classList.remove('is-drag');
          const file = (e.dataTransfer.files || [])[0];
          if (file) this.uploadAngle(key, file);
        });
        zone.addEventListener('click', (e) => {
          if (e.target.closest('button')) return;
          const input = qs(`[data-file-angle="${key}"]`, this.root);
          if (input) input.click();
        });
      });

      qsa('[data-clear-angle]', this.root).forEach(btn => {
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const key = btn.getAttribute('data-clear-angle');
          if (!confirm('Xóa góc này?')) return;
          await this.mutate('/angles/' + encodeURIComponent(key), { method: 'DELETE', label: 'Đang xóa…' });
        });
      });
    }

    bindGallerySort() {
      const wrap = qs('[data-sortable="gallery"]', this.root);
      if (!wrap) return;
      let dragEl = null;
      qsa('[draggable]', wrap).forEach(el => {
        el.addEventListener('dragstart', () => { dragEl = el; el.classList.add('is-dragging'); });
        el.addEventListener('dragend', async () => {
          el.classList.remove('is-dragging');
          dragEl = null;
          const ids = qsa('[data-id]', wrap).map(n => n.getAttribute('data-id'));
          await this.mutate('/gallery/reorder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids }),
            label: 'Đang lưu thứ tự…'
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
    }

    uploadSlot(slot, file) {
      const fd = new FormData();
      fd.append('file', file);
      return this.mutate('/' + slot, { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
    }

    clearSlot(slot) {
      return this.mutate('/' + slot, { method: 'DELETE', label: 'Đang xóa…' });
    }

    uploadGallery(files) {
      const fd = new FormData();
      files.forEach(f => fd.append('files', f));
      return this.mutate('/gallery', { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
    }

    uploadAngle(key, file) {
      const fd = new FormData();
      fd.append('file', file);
      return this.mutate('/angles/' + encodeURIComponent(key), { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    const root = qs('[data-media-studio]');
    if (!root) return;
    new MediaStudio(root).init();
  });
})();
