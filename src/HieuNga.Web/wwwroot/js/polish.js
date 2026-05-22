/**
 * Honda Hiếu Nga — global UX polish (HTMX, header, images, scroll reveal)
 */
(function () {
  'use strict';

  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ─── Top progress bar ─── */
  const progress = document.getElementById('htmx-progress');
  let progressTimer;

  function startProgress() {
    if (!progress || prefersReducedMotion) return;
    clearTimeout(progressTimer);
    progress.classList.add('is-active');
    progress.style.width = '0%';
    requestAnimationFrame(() => {
      progress.style.width = '70%';
    });
  }

  function finishProgress() {
    if (!progress) return;
    progress.style.width = '100%';
    progressTimer = setTimeout(() => {
      progress.classList.remove('is-active');
      progress.style.width = '0%';
    }, 280);
  }

  /* ─── HTMX lifecycle ─── */
  document.body.addEventListener('htmx:beforeRequest', startProgress);
  document.body.addEventListener('htmx:afterRequest', finishProgress);

  document.body.addEventListener('htmx:beforeSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'main-content') {
      target.classList.add('is-swapping');
    }
    if (target.id === 'catalog-grid' || target.id === 'blog-grid') {
      target.classList.add('is-loading');
    }
  });

  document.body.addEventListener('htmx:afterSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'main-content') {
      target.classList.remove('is-swapping');
      target.classList.add('page-enter');
      window.scrollTo({ top: 0, behavior: prefersReducedMotion ? 'auto' : 'smooth' });
      initImages(target);
      initReveals(target);
      if (window.Alpine) Alpine.initTree(target);
      const financeCfg = target.querySelector('#motorcycle-finance-config');
      if (financeCfg && window.bootMotorcycleFinance) {
        try {
          window.bootMotorcycleFinance(JSON.parse(financeCfg.textContent));
        } catch (_) { /* ignore */ }
      }
      setTimeout(() => target.classList.remove('page-enter'), 600);
      updateNavActive();
    } else {
      initImages(target);
      initReveals(target);
    }
    if (target.id === 'catalog-grid' || target.id === 'blog-grid') {
      target.classList.remove('is-loading');
    }
  });

  document.body.addEventListener('htmx:afterSettle', (e) => {
    initImages(e.detail.target);
  });

  /* ─── Sticky header shrink ─── */
  const header = document.getElementById('site-header');
  function onScroll() {
    if (!header) return;
    header.classList.toggle('is-scrolled', window.scrollY > 16);
  }

  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  /* ─── Image fade-in ─── */
  function initImages(root) {
    const scope = root || document;
    scope.querySelectorAll('img.img-media:not(.is-loaded)').forEach((img) => {
      const markLoaded = () => {
        img.classList.add('is-loaded');
        const wrap = img.closest('.img-wrap');
        if (wrap) wrap.classList.add('is-loaded');
      };
      if (img.complete && img.naturalWidth > 0) markLoaded();
      else {
        img.addEventListener('load', markLoaded, { once: true });
        img.addEventListener('error', markLoaded, { once: true });
      }
    });
  }

  /* ─── Scroll reveal ─── */
  let revealObserver;

  function initReveals(root) {
    if (prefersReducedMotion) {
      (root || document).querySelectorAll('.reveal').forEach((el) => el.classList.add('is-visible'));
      return;
    }
    if (!revealObserver) {
      revealObserver = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              entry.target.classList.add('is-visible');
              revealObserver.unobserve(entry.target);
            }
          });
        },
        { rootMargin: '0px 0px -6% 0px', threshold: 0.08 }
      );
    }
    const scope = root || document;
    scope.querySelectorAll('.reveal:not(.is-visible)').forEach((el) => revealObserver.observe(el));
  }

  /* ─── Active nav link ─── */
  function updateNavActive() {
    const path = window.location.pathname.replace(/\/$/, '') || '/';
    document.querySelectorAll('[data-nav]').forEach((link) => {
      const href = (link.getAttribute('href') || '').replace(/\/$/, '') || '/';
      const active = path === href || (href !== '/' && path.startsWith(href));
      link.classList.toggle('nav-link-active', active);
    });
  }

  /* ─── Touch feedback on mobile CTAs ─── */
  document.querySelectorAll('.touch-press').forEach((el) => {
    el.addEventListener('touchstart', () => el.classList.add('is-pressed'), { passive: true });
    el.addEventListener('touchend', () => el.classList.remove('is-pressed'), { passive: true });
  });

  /* ─── Motorcycle image error fallback ─── */
  const DEFAULT_MOTO_IMG = '/images/motorcycles/default.jpg';

  document.addEventListener(
    'error',
    (e) => {
      const img = e.target;
      if (!img || img.tagName !== 'IMG') return;
      if (!img.classList.contains('img-motorcycle') && !img.classList.contains('img-media')) return;
      const fb = img.dataset.fallback || DEFAULT_MOTO_IMG;
      if (!fb || img.src === fb) return;
      img.onerror = null;
      img.src = fb;
      img.classList.add('is-loaded');
      const wrap = img.closest('.img-wrap');
      if (wrap) wrap.classList.add('is-loaded');
    },
    true
  );

  /* ─── Init on first load ─── */
  document.addEventListener('DOMContentLoaded', () => {
    initImages(document);
    initReveals(document);
    updateNavActive();
  });
})();
