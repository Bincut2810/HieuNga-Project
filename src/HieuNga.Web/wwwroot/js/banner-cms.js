/**
 * Banner CMS — multi-image upload, reorder, remove.
 * Reuses Media Studio dropzone patterns and shared upload API.
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

  class BannerCms {
    constructor(root) {
      this.root = root;
      this.api = root.dataset.api;
      this.uploadEnabled = root.dataset.uploadEnabled !== 'false';
      this.state = null;
      this._ops = 0;
      this._dragEl = null;
    }

    async init() { await this.reload(); }

    async reload() {
      this.root.innerHTML = '<div class="ms-loading">Đang tải banner…</div>';
      try {
        const res = await fetch(this.api, { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Không tải được banner.');
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
      let el = qs('[data-bc-toast]', this.root);
      if (!el) {
        el = document.createElement('div');
        el.className = 'ms-toast';
        el.setAttribute('data-bc-toast', '');
        this.root.appendChild(el);
      }
      el.textContent = msg;
      el.classList.toggle('is-error', !!isError);
      el.hidden = false;
      clearTimeout(this._toastT);
      this._toastT = setTimeout(() => { el.hidden = true; }, 2800);
    }

    showProgress(on, label) {
      const bar = qs('[data-bc-progress]', this.root);
      if (!bar) return;
      bar.hidden = !on;
      const lab = qs('[data-bc-progress-label]', this.root);
      if (lab) lab.textContent = label || 'Đang xử lý…';
    }

    val(key) {
      const s = this.state || {};
      return s[key] ?? s[key.charAt(0).toUpperCase() + key.slice(1)];
    }

    images() {
      return this.val('images') || this.val('Images') || [];
    }

    render() {
      const title = this.val('title') || '';
      const subtitle = this.val('subtitle') || '';
      const enabled = this.val('enabled') !== false && this.val('Enabled') !== false;
      const images = this.images();

      this.root.innerHTML = `
        <div class="ms-progress" data-bc-progress hidden>
          <div class="ms-progress-bar"></div>
          <p data-bc-progress-label>Đang xử lý…</p>
        </div>
        <div class="ms-toast" data-bc-toast hidden></div>

        <section class="ms-card">
          <div class="ms-card-head">
            <h3>Ảnh banner</h3>
            <p class="ms-hint">Kéo nhiều ảnh vào khung — thứ tự = carousel trang chủ</p>
          </div>
          ${this.uploadEnabled
            ? `<div class="ms-dropzone ms-dropzone-lg" data-bc-drop tabindex="0">
                <div class="ms-drop-empty">
                  <strong>Kéo ảnh vào đây</strong>
                  <span>hoặc chạm để chọn — có thể chọn nhiều ảnh</span>
                </div>
                <input type="file" accept="image/*" multiple hidden data-bc-file />
              </div>`
            : '<p class="ms-hint ms-error">Tải ảnh chưa bật trên môi trường này.</p>'}
          <div class="bc-thumb-grid" data-bc-grid>
            ${images.map((img, i) => this.renderThumb(img, i)).join('')}
          </div>
        </section>

        <section class="ms-card admin-form-stack" style="margin-top:1rem;">
          <div>
            <label class="admin-label" for="bc-title">Tiêu đề</label>
            <input id="bc-title" class="admin-input" data-bc-title value="${esc(title)}" placeholder="Xe Máy Hiếu Nga" />
          </div>
          <div>
            <label class="admin-label" for="bc-subtitle">Phụ đề</label>
            <input id="bc-subtitle" class="admin-input" data-bc-subtitle value="${esc(subtitle)}" placeholder="Showroom Honda HEAD · Đà Nẵng" />
          </div>
          <label class="admin-check">
            <input type="checkbox" data-bc-enabled ${enabled ? 'checked' : ''} /> Xuất bản lên trang chủ
          </label>
          <div class="admin-form-actions">
            <button type="button" class="admin-btn admin-btn-primary ms-btn-lg" data-bc-save>Lưu</button>
          </div>
        </section>`;

      this.bindAfterRender();
    }

    renderThumb(img, index) {
      const id = img.id || img.Id;
      const url = img.url || img.Url;
      return `<article class="bc-thumb" draggable="true" data-bc-thumb data-id="${id}">
        <span class="bc-thumb-order">${index + 1}</span>
        <img src="${esc(url)}" alt="" />
        <button type="button" class="bc-thumb-remove ms-btn danger ms-btn-sm" data-bc-remove="${id}">Xóa</button>
      </article>`;
    }

    bindAfterRender() {
      const drop = qs('[data-bc-drop]', this.root);
      const fileInput = qs('[data-bc-file]', this.root);
      const grid = qs('[data-bc-grid]', this.root);

      if (drop && fileInput) {
        drop.addEventListener('dragover', (e) => { e.preventDefault(); drop.classList.add('is-drag'); });
        drop.addEventListener('dragleave', () => drop.classList.remove('is-drag'));
        drop.addEventListener('drop', (e) => {
          e.preventDefault();
          drop.classList.remove('is-drag');
          this.uploadFiles(Array.from(e.dataTransfer.files || []));
        });
        drop.addEventListener('click', (e) => {
          if (e.target.closest('button')) return;
          fileInput.click();
        });
        fileInput.addEventListener('change', () => {
          this.uploadFiles(Array.from(fileInput.files || []));
          fileInput.value = '';
        });
      }

      qsa('[data-bc-remove]', this.root).forEach((btn) => {
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const id = btn.getAttribute('data-bc-remove');
          if (!confirm('Xóa ảnh này?')) return;
          await this.mutate('/images/' + id, { method: 'DELETE', label: 'Đang xóa…' });
        });
      });

      if (grid) this.bindReorder(grid);

      qs('[data-bc-save]', this.root)?.addEventListener('click', () => this.saveSettings());
    }

    async uploadFiles(files) {
      const images = files.filter((f) => f.type.startsWith('image/'));
      if (!images.length) return;
      const fd = new FormData();
      images.forEach((f) => fd.append('files', f));
      await this.mutate('/images', { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
    }

    bindReorder(grid) {
      qsa('[data-bc-thumb]', grid).forEach((card) => {
        card.addEventListener('dragstart', () => {
          this._dragEl = card;
          card.classList.add('is-dragging');
        });
        card.addEventListener('dragend', async () => {
          card.classList.remove('is-dragging');
          this._dragEl = null;
          const ids = qsa('[data-id]', grid).map((el) => el.getAttribute('data-id'));
          await this.mutate('/reorder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids: ids }),
            label: 'Đang sắp xếp…'
          });
        });
        card.addEventListener('dragover', (e) => {
          e.preventDefault();
          if (!this._dragEl || this._dragEl === card) return;
          const rect = card.getBoundingClientRect();
          const before = (e.clientX - rect.left) < rect.width / 2;
          grid.insertBefore(this._dragEl, before ? card : card.nextSibling);
        });
      });
    }

    async saveSettings() {
      const title = qs('[data-bc-title]', this.root)?.value || '';
      const subtitle = qs('[data-bc-subtitle]', this.root)?.value || '';
      const enabled = qs('[data-bc-enabled]', this.root)?.checked ?? true;
      await this.mutate('/settings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: title, subtitle: subtitle, enabled: enabled }),
        label: 'Đang lưu…'
      });
    }
  }

  function boot() {
    const root = document.querySelector('[data-banner-cms]');
    if (!root || root.dataset.ready === '1') return;
    root.dataset.ready = '1';
    new BannerCms(root).init();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
