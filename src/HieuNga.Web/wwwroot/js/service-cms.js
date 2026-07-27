/**
 * Service CMS — multi-image upload, reorder, remove.
 * Same dropzone UX as Banner CMS / Media Studio.
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

  class ServiceCms {
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
      this.root.innerHTML = '<div class="ms-loading">Đang tải dịch vụ…</div>';
      try {
        const res = await fetch(this.api, { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Không tải được dịch vụ.');
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
      let el = qs('[data-sc-toast]', this.root);
      if (!el) {
        el = document.createElement('div');
        el.className = 'ms-toast';
        el.setAttribute('data-sc-toast', '');
        this.root.appendChild(el);
      }
      el.textContent = msg;
      el.classList.toggle('is-error', !!isError);
      el.hidden = false;
      clearTimeout(this._toastT);
      this._toastT = setTimeout(() => { el.hidden = true; }, 2800);
    }

    showProgress(on, label) {
      const bar = qs('[data-sc-progress]', this.root);
      if (!bar) return;
      bar.hidden = !on;
      const lab = qs('[data-sc-progress-label]', this.root);
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
      const name = this.val('name') || '';
      const shortDescription = this.val('shortDescription') || this.val('ShortDescription') || '';
      const displayOrder = this.val('displayOrder') ?? this.val('DisplayOrder') ?? 0;
      const enabled = this.val('enabled') !== false && this.val('Enabled') !== false;
      const images = this.images();

      this.root.innerHTML = `
        <div class="ms-progress" data-sc-progress hidden>
          <div class="ms-progress-bar"></div>
          <p data-sc-progress-label>Đang xử lý…</p>
        </div>
        <div class="ms-toast" data-sc-toast hidden></div>

        <section class="ms-card">
          <div class="ms-card-head">
            <h3>Ảnh dịch vụ</h3>
            <p class="ms-hint">Kéo ảnh vào khung — ảnh đầu tiên hiện trên danh sách</p>
          </div>
          ${this.uploadEnabled
            ? `<div class="ms-dropzone ms-dropzone-lg" data-sc-drop tabindex="0">
                <div class="ms-drop-empty">
                  <strong>Kéo ảnh vào đây</strong>
                  <span>hoặc chạm để chọn — có thể chọn nhiều ảnh</span>
                </div>
                <input type="file" accept="image/*" multiple hidden data-sc-file />
              </div>`
            : '<p class="ms-hint ms-error">Tải ảnh chưa bật trên môi trường này.</p>'}
          <div class="bc-thumb-grid" data-sc-grid>
            ${images.map((img, i) => this.renderThumb(img, i)).join('')}
          </div>
        </section>

        <section class="ms-card admin-form-stack" style="margin-top:1rem;">
          <div>
            <label class="admin-label" for="sc-name">Tên dịch vụ</label>
            <input id="sc-name" class="admin-input" data-sc-name value="${esc(name)}" />
          </div>
          <div>
            <label class="admin-label" for="sc-desc">Mô tả ngắn</label>
            <textarea id="sc-desc" class="admin-input" rows="3" data-sc-desc>${esc(shortDescription)}</textarea>
          </div>
          <div>
            <label class="admin-label" for="sc-order">Thứ tự hiển thị</label>
            <input id="sc-order" type="number" class="admin-input" data-sc-order value="${esc(String(displayOrder))}" />
          </div>
          <label class="admin-check">
            <input type="checkbox" data-sc-enabled ${enabled ? 'checked' : ''} /> Xuất bản lên website
          </label>
          <div class="admin-form-actions">
            <button type="button" class="admin-btn admin-btn-primary ms-btn-lg" data-sc-save>Lưu</button>
            <button type="button" class="admin-btn admin-btn-secondary ms-btn-lg" data-sc-publish>Lưu &amp; Xuất bản</button>
          </div>
        </section>`;

      this.bindAfterRender();
    }

    renderThumb(img, index) {
      const idx = img.index ?? img.Index ?? index;
      const url = img.url || img.Url;
      return `<article class="bc-thumb" draggable="true" data-sc-thumb data-index="${idx}">
        <span class="bc-thumb-order">${index + 1}</span>
        <img src="${esc(url)}" alt="" />
        <button type="button" class="bc-thumb-remove ms-btn danger ms-btn-sm" data-sc-remove="${idx}">Xóa</button>
      </article>`;
    }

    bindAfterRender() {
      const drop = qs('[data-sc-drop]', this.root);
      const fileInput = qs('[data-sc-file]', this.root);
      const grid = qs('[data-sc-grid]', this.root);

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

      qsa('[data-sc-remove]', this.root).forEach((btn) => {
        btn.addEventListener('click', async (e) => {
          e.stopPropagation();
          const index = btn.getAttribute('data-sc-remove');
          if (!confirm('Xóa ảnh này?')) return;
          await this.mutate('/images/' + index, { method: 'DELETE', label: 'Đang xóa…' });
        });
      });

      if (grid) this.bindReorder(grid);

      qs('[data-sc-save]', this.root)?.addEventListener('click', () => this.saveSettings(false));
      qs('[data-sc-publish]', this.root)?.addEventListener('click', () => this.saveSettings(true));
    }

    async uploadFiles(files) {
      const images = files.filter((f) => f.type.startsWith('image/'));
      if (!images.length) return;
      const fd = new FormData();
      images.forEach((f) => fd.append('files', f));
      await this.mutate('/images', { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
    }

    bindReorder(grid) {
      qsa('[data-sc-thumb]', grid).forEach((card) => {
        card.addEventListener('dragstart', () => {
          this._dragEl = card;
          card.classList.add('is-dragging');
        });
        card.addEventListener('dragend', async () => {
          card.classList.remove('is-dragging');
          this._dragEl = null;
          const indexes = qsa('[data-index]', grid).map((el) => parseInt(el.getAttribute('data-index'), 10));
          await this.mutate('/reorder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ indexes: indexes }),
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

    async saveSettings(forcePublish) {
      const name = qs('[data-sc-name]', this.root)?.value || '';
      const shortDescription = qs('[data-sc-desc]', this.root)?.value || '';
      const displayOrder = parseInt(qs('[data-sc-order]', this.root)?.value || '0', 10) || 0;
      let enabled = qs('[data-sc-enabled]', this.root)?.checked ?? false;
      if (forcePublish) {
        enabled = true;
        const box = qs('[data-sc-enabled]', this.root);
        if (box) box.checked = true;
      }
      await this.mutate('/settings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: name,
          shortDescription: shortDescription,
          displayOrder: displayOrder,
          enabled: enabled
        }),
        label: 'Đang lưu…'
      });
    }
  }

  function boot() {
    const root = document.querySelector('[data-service-cms]');
    if (!root || root.dataset.ready === '1') return;
    root.dataset.ready = '1';
    new ServiceCms(root).init();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
