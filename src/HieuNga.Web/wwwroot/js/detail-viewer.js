/**
 * Motorcycle detail — features, tech accordion, specs nav, sticky CTA.
 * Media (hero / gallery / colors / angles) lives in motorcycle-media-viewer.js.
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
