/**
 * MotorcycleMediaViewer V3 — hero from selected color, six angles.
 * Dataset: { thumbnail, colors[{id,name,hex,imageUrl}], angles[{angle,label,url}], name, storageKey }
 * Hero rule: selected color image → first color image → thumbnail → default
 */
(function () {
  'use strict';

  var PLACEHOLDER = '/images/motorcycles/default.svg';

  function warm(url) {
    if (!url) return;
    var img = new Image();
    img.decoding = 'async';
    img.src = url;
  }

  function parseMedia(el) {
    var raw = el.getAttribute('data-media');
    if (!raw) return null;
    try { return JSON.parse(raw); } catch (e) {
      console.warn('MotorcycleMediaViewer: invalid data-media', e);
      return null;
    }
  }

  class MotorcycleMediaViewer {
    constructor(root) {
      this.root = root;
      this.data = parseMedia(root);
      if (!this.data) return;

      this.thumbnail = this.data.thumbnail || PLACEHOLDER;
      this.colors = Array.isArray(this.data.colors) ? this.data.colors : [];
      this.angles = Array.isArray(this.data.angles) ? this.data.angles : [];
      this.name = this.data.name || '';
      this.storageKey = this.data.storageKey || '';
      this.selectedColorId = null;
      this.angleIndex = 0;
      this.drag = null;

      this.els = {
        hero: root.querySelector('[data-media-hero]'),
        skeleton: root.querySelector('[data-media-hero-skeleton]'),
        colorName: root.querySelector('[data-media-color-name]'),
        colorItems: root.querySelectorAll('[data-media-color-item]'),
        angleImg: root.querySelector('[data-media-angle-img]'),
        angleLabel: root.querySelector('[data-media-angle-label]'),
        angleHint: root.querySelector('[data-media-angle-hint]'),
        angleStage: root.querySelector('[data-media-angle-stage]'),
        angleTabs: root.querySelectorAll('[data-media-angle-tab]')
      };

      this.bind();
      this.restoreColor();
      this.setAngle(0);
      this.warmNeighbors();
    }

    resolveHero(color) {
      if (color && color.imageUrl) return color.imageUrl;
      var first = this.colors.find(function (c) { return c.imageUrl; });
      if (first && first.imageUrl) return first.imageUrl;
      return this.thumbnail || PLACEHOLDER;
    }

    bind() {
      var self = this;
      this.root.addEventListener('click', function (e) {
        var t = e.target.closest('[data-media-color-item]');
        if (t) {
          e.preventDefault();
          self.applyColor(t.getAttribute('data-id'));
          return;
        }
        t = e.target.closest('[data-media-angle-prev]');
        if (t) { e.preventDefault(); self.angleStep(-1); return; }
        t = e.target.closest('[data-media-angle-next]');
        if (t) { e.preventDefault(); self.angleStep(1); return; }
        t = e.target.closest('[data-media-angle-tab]');
        if (t) { e.preventDefault(); self.setAngle(Number(t.getAttribute('data-index') || 0)); return; }
        t = e.target.closest('[data-media-angle-fullscreen]');
        if (t) { e.preventDefault(); self.toggleFullscreen(); }
      });

      this._onKey = function (e) {
        if (!self.els.angleStage) return;
        if (e.key === 'ArrowRight') self.angleStep(1);
        else if (e.key === 'ArrowLeft') self.angleStep(-1);
      };
      document.addEventListener('keydown', this._onKey);

      if (this.els.angleStage) {
        this.els.angleStage.addEventListener('mousedown', function (e) { self.onAnglePointerDown(e); });
        window.addEventListener('mousemove', function (e) { self.onAnglePointerMove(e); });
        window.addEventListener('mouseup', function () { self.onAnglePointerUp(); });
        this.els.angleStage.addEventListener('touchstart', function (e) { self.onAnglePointerDown(e); }, { passive: true });
        this.els.angleStage.addEventListener('touchmove', function (e) { self.onAnglePointerMove(e); }, { passive: true });
        this.els.angleStage.addEventListener('touchend', function () { self.onAnglePointerUp(); });
      }
    }

    setHero(url) {
      if (!url || !this.els.hero) return;
      var self = this;
      if (this.els.skeleton) this.els.skeleton.hidden = false;
      this.els.hero.style.opacity = '0.55';
      this.els.hero.onload = function () {
        self.els.hero.style.opacity = '1';
        if (self.els.skeleton) self.els.skeleton.hidden = true;
      };
      this.els.hero.src = url;
      warm(url);
    }

    restoreColor() {
      var id = null;
      try {
        if (this.storageKey) id = sessionStorage.getItem(this.storageKey);
      } catch (e) { /* ignore */ }
      if (id && this.colors.some(function (c) { return c.id === id; })) this.applyColor(id, false);
      else if (this.colors[0]) this.applyColor(this.colors[0].id, false);
      else this.setHero(this.resolveHero(null));
    }

    applyColor(id, persist) {
      if (persist === undefined) persist = true;
      var color = this.colors.find(function (c) { return c.id === id; });
      if (!color) {
        this.setHero(this.resolveHero(null));
        return;
      }
      this.selectedColorId = id;
      this.setHero(this.resolveHero(color));
      if (this.els.colorName) this.els.colorName.textContent = color.name;
      this.els.colorItems.forEach(function (btn) {
        var on = btn.getAttribute('data-id') === id;
        btn.classList.toggle('is-selected', on);
        btn.setAttribute('aria-selected', on ? 'true' : 'false');
      });
      if (persist && this.storageKey) {
        try { sessionStorage.setItem(this.storageKey, id); } catch (e) { /* ignore */ }
      }
    }

    warmNeighbors() {
      var a = this.angles;
      if (!a.length) return;
      warm(a[this.angleIndex] && a[this.angleIndex].url);
      warm(a[(this.angleIndex + 1) % a.length] && a[(this.angleIndex + 1) % a.length].url);
      warm(a[(this.angleIndex - 1 + a.length) % a.length] && a[(this.angleIndex - 1 + a.length) % a.length].url);
    }

    setAngle(i) {
      if (!this.angles.length) return;
      if (i < 0 || i >= this.angles.length) return;
      this.angleIndex = i;
      var item = this.angles[i];
      if (this.els.angleImg && item) {
        this.els.angleImg.style.opacity = '0.55';
        this.els.angleImg.onload = () => { this.els.angleImg.style.opacity = '1'; };
        this.els.angleImg.src = item.url;
        this.els.angleImg.alt = (item.label || item.angle || '') + ' — ' + this.name;
      }
      if (this.els.angleLabel && item) this.els.angleLabel.textContent = item.label || item.angle || '';
      this.els.angleTabs.forEach(function (tab) {
        tab.classList.toggle('is-on', Number(tab.getAttribute('data-index')) === i);
      });
      if (this.els.angleHint) this.els.angleHint.hidden = true;
      this.warmNeighbors();
    }

    angleStep(delta) {
      if (this.angles.length < 2) return;
      this.setAngle((this.angleIndex + delta + this.angles.length) % this.angles.length);
    }

    onAnglePointerDown(e) {
      if (this.angles.length < 2) return;
      var x = e.clientX != null ? e.clientX : (e.touches && e.touches[0] && e.touches[0].clientX);
      this.drag = { startX: x || 0, lastIndex: this.angleIndex };
      if (this.els.angleHint) this.els.angleHint.hidden = true;
    }

    onAnglePointerMove(e) {
      if (!this.drag || this.angles.length < 2) return;
      var x = e.clientX != null ? e.clientX : (e.touches && e.touches[0] && e.touches[0].clientX);
      var delta = Math.round(((x || 0) - this.drag.startX) / 56);
      var next = (this.drag.lastIndex - delta) % this.angles.length;
      if (next < 0) next += this.angles.length;
      if (next !== this.angleIndex) this.setAngle(next);
    }

    onAnglePointerUp() { this.drag = null; }

    toggleFullscreen() {
      var el = this.els.angleStage;
      if (!el) return;
      if (!document.fullscreenElement) {
        var req = el.requestFullscreen || el.webkitRequestFullscreen;
        if (req) req.call(el);
      } else if (document.exitFullscreen) {
        document.exitFullscreen();
      }
    }
  }

  function boot() {
    document.querySelectorAll('[data-media-viewer]').forEach(function (el) {
      if (el.__mediaViewer) return;
      el.__mediaViewer = new MotorcycleMediaViewer(el);
    });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();

  window.MotorcycleMediaViewer = MotorcycleMediaViewer;
  window.bootMotorcycleMediaViewer = boot;
})();
