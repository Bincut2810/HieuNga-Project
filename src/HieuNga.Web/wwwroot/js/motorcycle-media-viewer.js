/**
 * MotorcycleMediaViewer — single public detail media controller.
 * Dataset: { hero, gallery[], angles[{angle,label,url}], colors[{id,name,hex,imageUrl}], name, storageKey }
 * Warms only current / previous / next. No spin / FrameIndex / preload queues.
 */
(function () {
  'use strict';

  function warm(url) {
    if (!url) return;
    var img = new Image();
    img.decoding = 'async';
    img.src = url;
  }

  function parseMedia(el) {
    var raw = el.getAttribute('data-media');
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch (e) {
      console.warn('MotorcycleMediaViewer: invalid data-media', e);
      return null;
    }
  }

  class MotorcycleMediaViewer {
    constructor(root) {
      this.root = root;
      this.data = parseMedia(root);
      if (!this.data) return;

      this.hero = this.data.hero || '';
      this.gallery = Array.isArray(this.data.gallery) ? this.data.gallery : [];
      this.angles = Array.isArray(this.data.angles) ? this.data.angles : [];
      this.colors = Array.isArray(this.data.colors) ? this.data.colors : [];
      this.name = this.data.name || '';
      this.storageKey = this.data.storageKey || '';

      this.galleryIndex = Math.max(0, this.gallery.indexOf(this.hero));
      if (this.galleryIndex < 0) this.galleryIndex = 0;
      this.angleIndex = 0;
      this.lightboxOpen = false;
      this.drag = null;
      this.selectedColorId = null;

      this.els = {
        hero: root.querySelector('[data-media-hero]'),
        skeleton: root.querySelector('[data-media-hero-skeleton]'),
        galleryItems: root.querySelectorAll('[data-media-gallery-item]'),
        lightbox: root.querySelector('[data-media-lightbox]'),
        lightboxImg: root.querySelector('[data-media-lightbox-img]'),
        lightboxCounter: root.querySelector('[data-media-lightbox-counter]'),
        colorPreview: root.querySelector('[data-media-color-preview]'),
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
      this.setHero(this.hero, this.galleryIndex);
      this.setAngle(0);
      this.warmNeighbors();
    }

    bind() {
      var self = this;
      this.root.addEventListener('click', function (e) {
        var t = e.target.closest('[data-media-open-lightbox]');
        if (t) {
          e.preventDefault();
          self.openLightbox(self.galleryIndex);
          return;
        }
        t = e.target.closest('[data-media-gallery-item]');
        if (t) {
          e.preventDefault();
          var idx = Number(t.getAttribute('data-index') || 0);
          var url = t.getAttribute('data-url') || self.gallery[idx];
          self.setHero(url, idx);
          return;
        }
        t = e.target.closest('[data-media-lightbox-close]');
        if (t) {
          e.preventDefault();
          self.closeLightbox();
          return;
        }
        t = e.target.closest('[data-media-lightbox-prev]');
        if (t) {
          e.preventDefault();
          self.lightboxStep(-1);
          return;
        }
        t = e.target.closest('[data-media-lightbox-next]');
        if (t) {
          e.preventDefault();
          self.lightboxStep(1);
          return;
        }
        t = e.target.closest('[data-media-color-item]');
        if (t) {
          e.preventDefault();
          self.applyColor(t.getAttribute('data-id'));
          return;
        }
        t = e.target.closest('[data-media-angle-prev]');
        if (t) {
          e.preventDefault();
          self.angleStep(-1);
          return;
        }
        t = e.target.closest('[data-media-angle-next]');
        if (t) {
          e.preventDefault();
          self.angleStep(1);
          return;
        }
        t = e.target.closest('[data-media-angle-tab]');
        if (t) {
          e.preventDefault();
          self.setAngle(Number(t.getAttribute('data-index') || 0));
          return;
        }
        t = e.target.closest('[data-media-angle-fullscreen]');
        if (t) {
          e.preventDefault();
          self.toggleFullscreen();
        }
      });

      this._onKey = function (e) {
        if (self.lightboxOpen) {
          if (e.key === 'Escape') self.closeLightbox();
          else if (e.key === 'ArrowRight') self.lightboxStep(1);
          else if (e.key === 'ArrowLeft') self.lightboxStep(-1);
          return;
        }
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

      if (this.els.lightbox) {
        var panel = this.els.lightbox.querySelector('.detail-lightbox-panel');
        var touchX = null;
        if (panel) {
          panel.addEventListener('touchstart', function (e) {
            touchX = e.changedTouches[0] ? e.changedTouches[0].clientX : null;
          }, { passive: true });
          panel.addEventListener('touchend', function (e) {
            if (touchX == null) return;
            var dx = (e.changedTouches[0] ? e.changedTouches[0].clientX : touchX) - touchX;
            if (Math.abs(dx) > 40) self.lightboxStep(dx < 0 ? 1 : -1);
            touchX = null;
          }, { passive: true });
        }
      }
    }

    setHero(url, index) {
      if (!url) return;
      this.hero = url;
      if (typeof index === 'number') this.galleryIndex = index;
      if (this.els.hero) {
        if (this.els.skeleton) this.els.skeleton.hidden = false;
        this.els.hero.onload = () => {
          if (this.els.skeleton) this.els.skeleton.hidden = true;
        };
        this.els.hero.src = url;
      }
      this.els.galleryItems.forEach(function (btn) {
        var active = btn.getAttribute('data-url') === url;
        btn.classList.toggle('border-honda-red', active);
        btn.classList.toggle('ring-2', active);
        btn.classList.toggle('ring-honda-red/20', active);
        btn.classList.toggle('border-transparent', !active);
        btn.classList.toggle('opacity-80', !active);
        btn.setAttribute('aria-current', active ? 'true' : 'false');
      });
      this.warmNeighbors();
    }

    warmNeighbors() {
      var g = this.gallery;
      if (g.length) {
        warm(g[this.galleryIndex]);
        warm(g[(this.galleryIndex + 1) % g.length]);
        warm(g[(this.galleryIndex - 1 + g.length) % g.length]);
      }
      var a = this.angles;
      if (a.length) {
        warm(a[this.angleIndex] && a[this.angleIndex].url);
        warm(a[(this.angleIndex + 1) % a.length] && a[(this.angleIndex + 1) % a.length].url);
        warm(a[(this.angleIndex - 1 + a.length) % a.length] && a[(this.angleIndex - 1 + a.length) % a.length].url);
      }
    }

    openLightbox(index) {
      if (!this.els.lightbox || !this.gallery.length) return;
      this.galleryIndex = typeof index === 'number' ? index : this.galleryIndex;
      this.lightboxOpen = true;
      this.els.lightbox.hidden = false;
      document.body.classList.add('detail-lightbox-open');
      this.renderLightbox();
    }

    closeLightbox() {
      this.lightboxOpen = false;
      if (this.els.lightbox) this.els.lightbox.hidden = true;
      document.body.classList.remove('detail-lightbox-open');
    }

    lightboxStep(delta) {
      if (!this.gallery.length) return;
      this.galleryIndex = (this.galleryIndex + delta + this.gallery.length) % this.gallery.length;
      this.setHero(this.gallery[this.galleryIndex], this.galleryIndex);
      this.renderLightbox();
    }

    renderLightbox() {
      var url = this.gallery[this.galleryIndex] || this.hero;
      if (this.els.lightboxImg) {
        this.els.lightboxImg.style.opacity = '0.55';
        this.els.lightboxImg.onload = () => {
          this.els.lightboxImg.style.opacity = '1';
        };
        this.els.lightboxImg.src = url;
        this.els.lightboxImg.alt = this.name + ' — ảnh ' + (this.galleryIndex + 1);
      }
      if (this.els.lightboxCounter) {
        this.els.lightboxCounter.textContent = (this.galleryIndex + 1) + ' / ' + this.gallery.length;
      }
    }

    restoreColor() {
      var id = null;
      try {
        if (this.storageKey) id = sessionStorage.getItem(this.storageKey);
      } catch (e) { /* ignore */ }
      if (id && this.colors.some(function (c) { return c.id === id; })) this.applyColor(id, false);
      else if (this.colors[0]) this.applyColor(this.colors[0].id, false);
    }

    applyColor(id, persist) {
      if (persist === undefined) persist = true;
      var color = this.colors.find(function (c) { return c.id === id; });
      if (!color) return;
      this.selectedColorId = id;
      if (color.imageUrl) {
        this.setHero(color.imageUrl, this.gallery.indexOf(color.imageUrl));
        if (this.els.colorPreview) this.els.colorPreview.src = color.imageUrl;
      }
      if (this.els.colorName) this.els.colorName.textContent = color.name;
      this.els.colorItems.forEach(function (btn) {
        var on = btn.getAttribute('data-id') === id;
        btn.classList.toggle('is-selected', on);
        btn.setAttribute('aria-selected', on ? 'true' : 'false');
        var hint = btn.querySelector('[data-media-color-hint]');
        if (hint) hint.hidden = !on;
      });
      if (persist && this.storageKey) {
        try { sessionStorage.setItem(this.storageKey, id); } catch (e) { /* ignore */ }
      }
    }

    setAngle(i) {
      if (!this.angles.length) return;
      if (i < 0 || i >= this.angles.length) return;
      this.angleIndex = i;
      var item = this.angles[i];
      if (this.els.angleImg && item) {
        this.els.angleImg.style.opacity = '0.55';
        this.els.angleImg.onload = () => {
          this.els.angleImg.style.opacity = '1';
        };
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

    onAnglePointerUp() {
      this.drag = null;
    }

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

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  window.MotorcycleMediaViewer = MotorcycleMediaViewer;
  window.bootMotorcycleMediaViewer = boot;
})();
