/**
 * Motorcycle detail — color hero, 360 viewer, feature showcase (Alpine).
 * Installment calculator stays in detail-finance.js (unchanged formula).
 */
(function () {
  'use strict';

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
      init() {
        let restored = null;
        try {
          if (this.storageKey) restored = sessionStorage.getItem(this.storageKey);
        } catch (_) {
          /* ignore */
        }
        if (restored && this.colors.some((c) => c.id === restored)) {
          this.selectedId = restored;
        } else if (!this.selectedId && this.colors.length) {
          this.selectedId = this.colors[0].id;
        }
        this.applyColor(this.selectedId, false);
        preloadImages([
          ...this.colors.map((c) => c.imageUrl),
          ...this.gallery
        ]);
      },
      get selected() {
        return this.colors.find((c) => c.id === this.selectedId) || this.colors[0] || null;
      },
      applyColor(id, persist = true) {
        this.selectedId = id;
        const c = this.selected;
        if (c && c.imageUrl) {
          this.heroReady = this.heroSrc === c.imageUrl;
          this.heroSrc = c.imageUrl;
        }
        if (persist && this.storageKey && id) {
          try {
            sessionStorage.setItem(this.storageKey, id);
          } catch (_) {
            /* ignore */
          }
        }
      },
      selectGallery(url) {
        if (!url) return;
        this.heroReady = this.heroSrc === url;
        this.heroSrc = url;
        const match = this.colors.find((c) => c.imageUrl === url);
        if (match) this.applyColor(match.id);
      }
    }));

    Alpine.data('detailSpin360', (frames) => ({
      frames: Array.isArray(frames) ? frames : [],
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
        const firstBatch = this.frames.slice(0, Math.min(12, this.frames.length));
        preloadImages(firstBatch, (ratio) => {
          if (ratio >= 1) this.loading = false;
        }).then(() => {
          this.loading = false;
          this.hintTimer = setTimeout(() => {
            this.showHint = false;
          }, 3200);
        });

        const io =
          typeof IntersectionObserver !== 'undefined'
            ? new IntersectionObserver(
                (entries) => {
                  if (entries.some((e) => e.isIntersecting)) {
                    preloadImages(this.frames);
                    io.disconnect();
                  }
                },
                { rootMargin: '240px' }
              )
            : null;
        this.$nextTick(() => {
          const el = this.$refs.stage || this.$el;
          if (io && el) io.observe(el);
          else preloadImages(this.frames);
        });
      },
      get src() {
        return this.frames[this.index] || '';
      },
      get ready() {
        return this.frames.length >= 2;
      },
      onPointerDown(e) {
        if (!this.ready || this.loading) return;
        this.dragging = true;
        this.showHint = false;
        this.startX = e.clientX ?? (e.touches && e.touches[0]?.clientX) || 0;
        this.lastIndex = this.index;
      },
      onPointerMove(e) {
        if (!this.dragging || !this.ready) return;
        const x = e.clientX ?? (e.touches && e.touches[0]?.clientX) || 0;
        const dx = x - this.startX;
        const step = Math.max(8, Math.floor(240 / this.frames.length));
        const delta = Math.round(dx / step);
        let next = (this.lastIndex - delta) % this.frames.length;
        if (next < 0) next += this.frames.length;
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

    Alpine.data('detailSpecsAccordion', () => ({
      open: typeof window !== 'undefined' && window.matchMedia('(min-width: 768px)').matches
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
          { threshold: 0.08, rootMargin: '-48px 0px 0px 0px' }
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
