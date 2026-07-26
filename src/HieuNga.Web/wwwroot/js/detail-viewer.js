/**
 * Motorcycle detail V2 — color/gallery lightbox, six-angle viewer, features, specs nav, sticky CTA.
 * Detail installment UI lives in ~/js/finance-calculator.js (layout module).
 */
(function () {
  'use strict';

  function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  function preloadImages(urls, onProgress) {
    const list = (urls || []).filter((u) => typeof u === 'string' && u);
    if (!list.length) {
      if (onProgress) onProgress(1, 0, 0);
      return Promise.resolve();
    }
    let done = 0;
    return Promise.all(
      list.map(
        (url) =>
          new Promise((resolve) => {
            const img = new Image();
            img.decoding = 'async';
            const finish = () => {
              done += 1;
              if (onProgress) onProgress(done / list.length, done, list.length);
              resolve();
            };
            img.onload = finish;
            img.onerror = finish;
            img.src = url;
          })
      )
    );
  }

  function register() {
    if (typeof Alpine === 'undefined') return;

    Alpine.data('detailColorHero', (config) => ({
      colors: config.colors || [],
      gallery: config.gallery || [],
      selectedId: config.selectedId || null,
      heroSrc: config.heroSrc || '',
      name: config.name || '',
      storageKey: config.storageKey || '',
      heroReady: false,
      lightboxOpen: false,
      lightboxIndex: 0,
      touchX: null,

      init() {
        let restored = null;
        try {
          if (this.storageKey) restored = sessionStorage.getItem(this.storageKey);
        } catch (_) { /* ignore */ }
        if (restored && this.colors.some((c) => c.id === restored)) {
          this.selectedId = restored;
        } else if (!this.selectedId && this.colors.length) {
          this.selectedId = this.colors[0].id;
        }
        this.applyColor(this.selectedId, false);
        preloadImages([
          ...this.colors.map((c) => c.imageUrl),
          ...this.gallery.slice(0, 6)
        ]);

        this._onKey = (e) => {
          if (!this.lightboxOpen) return;
          if (e.key === 'Escape') this.closeLightbox();
          else if (e.key === 'ArrowRight') this.lightboxNext();
          else if (e.key === 'ArrowLeft') this.lightboxPrev();
        };
        document.addEventListener('keydown', this._onKey);
      },
      destroy() {
        document.removeEventListener('keydown', this._onKey);
      },
      get selected() {
        return this.colors.find((c) => c.id === this.selectedId) || this.colors[0] || null;
      },
      get lightboxSrc() {
        return this.gallery[this.lightboxIndex] || this.heroSrc;
      },
      applyColor(id, persist = true) {
        if (!id && this.colors.length) id = this.colors[0].id;
        this.selectedId = id;
        const c = this.selected;
        if (c && c.imageUrl) {
          this.heroReady = this.heroSrc === c.imageUrl;
          this.heroSrc = c.imageUrl;
          const gi = this.gallery.indexOf(c.imageUrl);
          if (gi >= 0) this.lightboxIndex = gi;
        }
        if (persist && this.storageKey && id) {
          try { sessionStorage.setItem(this.storageKey, id); } catch (_) { /* ignore */ }
        }
        this.$dispatch('detail-color-changed', { id, imageUrl: c?.imageUrl || null });
      },
      selectGallery(url, index) {
        if (!url) return;
        this.heroReady = this.heroSrc === url;
        this.heroSrc = url;
        if (typeof index === 'number') this.lightboxIndex = index;
        else {
          const gi = this.gallery.indexOf(url);
          if (gi >= 0) this.lightboxIndex = gi;
        }
        const match = this.colors.find((c) => c.imageUrl === url);
        if (match) this.applyColor(match.id);
      },
      openLightbox(index) {
        if (typeof index === 'number') this.lightboxIndex = index;
        else {
          const gi = this.gallery.indexOf(this.heroSrc);
          this.lightboxIndex = gi >= 0 ? gi : 0;
        }
        this.lightboxOpen = true;
        document.body.classList.add('detail-lightbox-open');
      },
      closeLightbox() {
        this.lightboxOpen = false;
        document.body.classList.remove('detail-lightbox-open');
      },
      lightboxNext() {
        if (!this.gallery.length) return;
        this.lightboxIndex = (this.lightboxIndex + 1) % this.gallery.length;
        this.heroSrc = this.gallery[this.lightboxIndex];
      },
      lightboxPrev() {
        if (!this.gallery.length) return;
        this.lightboxIndex = (this.lightboxIndex - 1 + this.gallery.length) % this.gallery.length;
        this.heroSrc = this.gallery[this.lightboxIndex];
      },
      onLightboxTouchStart(e) {
        this.touchX = e.changedTouches[0]?.clientX ?? null;
      },
      onLightboxTouchEnd(e) {
        if (this.touchX == null) return;
        const dx = (e.changedTouches[0]?.clientX ?? this.touchX) - this.touchX;
        if (Math.abs(dx) > 40) {
          if (dx < 0) this.lightboxNext();
          else this.lightboxPrev();
        }
        this.touchX = null;
      }
    }));

    Alpine.data('detailAngleViewer', (items) => ({
      angles: Array.isArray(items) ? items.filter((x) => x && x.url) : [],
      index: 0,
      dragging: false,
      startX: 0,
      lastIndex: 0,
      loading: true,
      showHint: true,
      hintTimer: null,

      init() {
        if (!this.ready) {
          this.loading = false;
          return;
        }
        const urls = this.angles.map((a) => a.url);
        preloadImages(urls.slice(0, 2)).then(() => {
          this.loading = false;
          this.hintTimer = setTimeout(() => { this.showHint = false; }, 3200);
        });
        const io =
          typeof IntersectionObserver !== 'undefined'
            ? new IntersectionObserver(
                (entries) => {
                  if (entries.some((e) => e.isIntersecting)) {
                    preloadImages(urls);
                    io.disconnect();
                  }
                },
                { rootMargin: '200px' }
              )
            : null;
        this.$nextTick(() => {
          const el = this.$refs.stage || this.$el;
          if (io && el) io.observe(el);
          else preloadImages(urls);
        });
      },
      get src() {
        return (this.angles[this.index] && this.angles[this.index].url) || '';
      },
      get label() {
        const a = this.angles[this.index];
        return a ? (a.label || a.angle || '') : '';
      },
      get ready() {
        return this.angles.length >= 2;
      },
      go(i) {
        if (i < 0 || i >= this.angles.length) return;
        this.index = i;
        this.showHint = false;
      },
      next() {
        if (!this.ready) return;
        this.index = (this.index + 1) % this.angles.length;
      },
      prev() {
        if (!this.ready) return;
        this.index = (this.index - 1 + this.angles.length) % this.angles.length;
      },
      onPointerDown(e) {
        if (!this.ready || this.loading) return;
        this.dragging = true;
        this.showHint = false;
        this.startX = e.clientX ?? (e.touches && e.touches[0] && e.touches[0].clientX) || 0;
        this.lastIndex = this.index;
      },
      onPointerMove(e) {
        if (!this.dragging || !this.ready) return;
        const x = e.clientX ?? (e.touches && e.touches[0] && e.touches[0].clientX) || 0;
        const dx = x - this.startX;
        const step = 56;
        const delta = Math.round(dx / step);
        let next = (this.lastIndex - delta) % this.angles.length;
        if (next < 0) next += this.angles.length;
        this.index = next;
      },
      onPointerUp() {
        this.dragging = false;
      },
      toggleFullscreen() {
        const el = this.$refs.stage;
        if (!el) return;
        if (!document.fullscreenElement) {
          const req = el.requestFullscreen || el.webkitRequestFullscreen;
          if (req) req.call(el);
        } else if (document.exitFullscreen) {
          document.exitFullscreen();
        }
      }
    }));

    Alpine.data('detailFeatureShowcase', (items) => ({
      items: Array.isArray(items) ? items : [],
      active: 0,
      init() {
        preloadImages(this.items.map((i) => i.imageUrl).filter(Boolean).slice(0, 4));
      },
      get current() {
        return this.items[this.active] || null;
      },
      next() {
        if (!this.items.length) return;
        this.active = (this.active + 1) % this.items.length;
      },
      prev() {
        if (!this.items.length) return;
        this.active = (this.active - 1 + this.items.length) % this.items.length;
      },
      go(i) {
        this.active = i;
      }
    }));

    Alpine.data('detailTechAccordion', () => ({
      openId: null,
      init() {
        const first = this.$el.querySelector('[data-tech-id]');
        if (first) this.openId = first.getAttribute('data-tech-id');
      },
      isOpen(id) {
        return this.openId === id;
      },
      toggle(id) {
        this.openId = this.openId === id ? null : id;
      }
    }));

    Alpine.data('detailSpecsNav', () => ({
      active: '',
      init() {
        const links = Array.from(this.$el.querySelectorAll('[data-spec-nav]'));
        const targets = links
          .map((a) => document.getElementById(a.getAttribute('href')?.slice(1) || ''))
          .filter(Boolean);
        if (!targets.length || typeof IntersectionObserver === 'undefined') return;
        const io = new IntersectionObserver(
          (entries) => {
            const visible = entries
              .filter((e) => e.isIntersecting)
              .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
            if (visible?.target?.id) this.active = visible.target.id;
          },
          { rootMargin: '-20% 0px -55% 0px', threshold: [0.1, 0.4] }
        );
        targets.forEach((t) => io.observe(t));
        if (targets[0]) this.active = targets[0].id;
      },
      go(id, e) {
        if (e) e.preventDefault();
        const el = document.getElementById(id);
        if (!el) return;
        this.active = id;
        el.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'start' });
      }
    }));

    Alpine.data('detailStickyCta', () => ({
      visible: false,
      init() {
        const hero = document.querySelector('.detail-hero');
        if (!hero || typeof IntersectionObserver === 'undefined') {
          this.visible = true;
          return;
        }
        const io = new IntersectionObserver(
          ([entry]) => {
            this.visible = !entry.isIntersecting;
            document.body.classList.toggle('detail-sticky-visible', this.visible);
          },
          { threshold: 0.05, rootMargin: '-40px 0px 0px 0px' }
        );
        io.observe(hero);
      }
    }));
  }

  if (!window.__hnDetailViewerBooted) {
    window.__hnDetailViewerBooted = true;
    document.addEventListener('alpine:init', register);
  }
  if (typeof Alpine !== 'undefined') register();
  window.registerMotorcycleDetailUi = register;
})();
