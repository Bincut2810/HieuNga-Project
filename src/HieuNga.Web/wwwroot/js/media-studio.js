/**
 * Hình ảnh xe V3 — Ảnh đại diện · Màu xe · 6 góc xe.
 * Drop = upload + save. No Gallery. No Hero slot.
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

    async init() { await this.reload(); }

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
      const colors = s.colors || s.Colors || [];
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
            <p class="ms-sub">Kéo ảnh vào khung — hệ thống tự lưu.</p>
          </div>
        </header>
        <section class="ms-complete ${ready ? 'is-ready' : 'is-warn'}">
          <strong>${ready ? '✓ Đủ hình để đăng' : 'Hoàn thiện hình ảnh'}</strong>
          <ul>${missing.length ? missing.map(m => `<li>${esc(m)}</li>`).join('') : '<li>Ảnh đại diện · Màu xe · 6 góc (tuỳ chọn)</li>'}</ul>
        </section>
        ${this.renderAvatar(thumb)}
        ${this.renderColors(colors)}
        ${this.renderAngles(angles)}
      `;
      this.bindAfterRender();
    }

    renderAvatar(data) {
      const url = data && (data.url || data.Url);
      return `
        <section class="ms-card">
          <div class="ms-card-head">
            <h3>1. Ảnh đại diện</h3>
            <p class="ms-hint">Một ảnh — hiện trên danh sách & trang chủ</p>
          </div>
          <div class="ms-dropzone ms-dropzone-lg ${url ? 'has-image' : ''}" data-drop="thumbnail" tabindex="0">
            ${url
              ? `<img src="${esc(url)}" alt="" class="ms-slot-img" />`
              : `<div class="ms-drop-empty"><strong>Kéo ảnh vào đây</strong><span>hoặc chạm để chọn</span></div>`}
            <input type="file" accept="image/*" hidden data-file="thumbnail" />
          </div>
          <div class="ms-actions">
            <button type="button" class="ms-btn primary ms-btn-lg" data-pick="thumbnail">${url ? 'Đổi ảnh' : 'Chọn ảnh'}</button>
            ${url ? `<button type="button" class="ms-btn danger ms-btn-lg" data-clear="thumbnail">Xóa</button>` : ''}
          </div>
        </section>`;
    }

    renderColors(colors) {
      return `
        <section class="ms-card">
          <div class="ms-card-head row">
            <div>
              <h3>2. Màu xe <span class="ms-count">${colors.length}</span></h3>
              <p class="ms-hint">Mỗi màu một ảnh — ảnh này là ảnh chính trên trang chi tiết</p>
            </div>
            <button type="button" class="ms-btn primary" data-color-add>+ Thêm màu</button>
          </div>
          <div class="ms-color-grid">
            ${colors.map(c => {
              const id = c.id || c.Id;
              const img = c.imageUrl || c.ImageUrl;
              return `<article class="ms-color-card" data-color-id="${id}">
                <div class="ms-color-swatch" style="background:${esc(c.hexCode || c.HexCode)}"></div>
                ${img
                  ? `<img src="${esc(img)}" alt="" />`
                  : `<div class="ms-color-empty">Chưa có ảnh</div>`}
                <div class="ms-color-body">
                  <strong>${esc(c.name || c.Name)}</strong>
                  <span>${esc(c.hexCode || c.HexCode)}</span>
                </div>
                <div class="ms-color-actions">
                  <label class="ms-btn primary ms-btn-sm">
                    ${img ? 'Đổi ảnh' : 'Thêm ảnh'}
                    <input type="file" accept="image/*" hidden data-color-image="${id}" />
                  </label>
                  <button type="button" class="ms-btn danger ms-btn-sm" data-color-delete="${id}">Xóa</button>
                </div>
              </article>`;
            }).join('') || '<p class="ms-hint">Chưa có màu — bấm Thêm màu.</p>'}
          </div>
          <dialog class="ms-dialog" data-color-dialog>
            <form method="dialog" class="ms-dialog-form" data-color-form>
              <h3>Thêm màu</h3>
              <label>Tên màu<input name="name" required placeholder="Đen bóng" /></label>
              <label>Mã màu<input name="hex" required placeholder="#111111" value="#111111" /></label>
              <label>Ảnh màu<input type="file" name="image" accept="image/*" required /></label>
              <div class="ms-actions">
                <button type="submit" class="ms-btn primary">Lưu</button>
                <button type="button" class="ms-btn" data-color-close>Đóng</button>
              </div>
            </form>
          </dialog>
        </section>`;
    }

    renderAngles(angles) {
      const slots = (angles && (angles.slots || angles.Slots)) || [];
      const filled = angles && (angles.filledCount ?? angles.FilledCount) || 0;
      return `
        <section class="ms-card">
          <div class="ms-card-head">
            <h3>3. 6 góc xe <span class="ms-count">${filled}/6</span></h3>
            <p class="ms-hint">Tuỳ chọn — kéo ảnh vào từng ô</p>
          </div>
          <div class="ms-angle-grid">
            ${slots.map(slot => {
              const key = slot.key || slot.Key;
              const url = slot.url || slot.Url;
              const lab = slot.label || slot.Label;
              return `<article class="ms-angle-slot">
                <div class="ms-dropzone ms-angle-drop ${url ? 'has-image' : ''}" data-drop-angle="${esc(key)}" tabindex="0">
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
            }).join('')}
          </div>
        </section>`;
    }

    bindAfterRender() {
      qsa('[data-pick]', this.root).forEach(btn => {
        btn.addEventListener('click', () => {
          const input = qs(`[data-file="${btn.getAttribute('data-pick')}"]`, this.root);
          if (input) input.click();
        });
      });

      qsa('[data-file]', this.root).forEach(input => {
        input.addEventListener('change', () => {
          const key = input.getAttribute('data-file');
          const files = Array.from(input.files || []);
          input.value = '';
          if (key === 'thumbnail' && files[0]) this.uploadSlot(files[0]);
        });
      });

      qsa('[data-drop="thumbnail"]', this.root).forEach(zone => {
        zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('is-drag'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('is-drag'));
        zone.addEventListener('drop', (e) => {
          e.preventDefault();
          zone.classList.remove('is-drag');
          const file = (e.dataTransfer.files || [])[0];
          if (file) this.uploadSlot(file);
        });
        zone.addEventListener('click', (e) => {
          if (e.target.closest('button,input')) return;
          const input = qs('[data-file="thumbnail"]', this.root);
          if (input) input.click();
        });
      });

      qs('[data-clear="thumbnail"]', this.root)?.addEventListener('click', () => {
        if (!confirm('Xóa ảnh đại diện?')) return;
        this.mutate('/thumbnail', { method: 'DELETE', label: 'Đang xóa…' });
      });

      this.bindColors();
      this.bindAngles();
    }

    bindColors() {
      const dialog = qs('[data-color-dialog]', this.root);
      const form = qs('[data-color-form]', this.root);
      qs('[data-color-add]', this.root)?.addEventListener('click', () => {
        form.reset();
        dialog.showModal();
      });
      qs('[data-color-close]', this.root)?.addEventListener('click', () => dialog.close());
      form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData();
        fd.append('name', form.name.value);
        fd.append('hex', form.hex.value);
        if (form.image.files[0]) fd.append('image', form.image.files[0]);
        dialog.close();
        await this.mutate('/colors', { method: 'POST', body: fd, label: 'Đang lưu màu…' });
      });

      qsa('[data-color-image]', this.root).forEach(input => {
        input.addEventListener('change', async () => {
          const id = input.getAttribute('data-color-image');
          const file = (input.files || [])[0];
          input.value = '';
          if (!file || !id) return;
          const fd = new FormData();
          fd.append('file', file);
          await this.mutate('/colors/' + id + '/image', { method: 'POST', body: fd, label: 'Đang tải ảnh màu…' });
        });
      });

      qsa('[data-color-delete]', this.root).forEach(btn => {
        btn.addEventListener('click', async () => {
          const id = btn.getAttribute('data-color-delete');
          if (!id || !confirm('Xóa màu này?')) return;
          await this.mutate('/colors/' + id, { method: 'DELETE', label: 'Đang xóa…' });
        });
      });
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
        zone.addEventListener('click', () => {
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

    uploadSlot(file) {
      const fd = new FormData();
      fd.append('file', file);
      return this.mutate('/thumbnail', { method: 'POST', body: fd, label: 'Đang tải ảnh…' });
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
