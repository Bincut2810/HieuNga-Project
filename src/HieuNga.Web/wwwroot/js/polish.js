/**
 * Honda Hiếu Nga — global UX polish (HTMX app navigation, header, images)
 */
(function () {
  'use strict';

  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const header = document.getElementById('site-header');

  /* ─── HTMX: exclude unsafe links from boost ─── */
  function shouldExcludeBoost(anchor) {
    const href = (anchor.getAttribute('href') || '').trim();
    if (!href || href === '#') return true;
    if (href.startsWith('#')) return true;
    if (href.startsWith('tel:') || href.startsWith('mailto:')) return true;
    if (href.startsWith('http://') || href.startsWith('https://') || href.startsWith('//')) return true;
    if (href.startsWith('/admin') || href.startsWith('admin/')) return true;
    if (anchor.hasAttribute('download')) return true;
    if (anchor.target === '_blank' || anchor.target === '_top') return true;
    if (anchor.getAttribute('hx-boost') === 'false') return true;
    return false;
  }

  document.body.addEventListener('htmx:beforeProcessNode', (e) => {
    const el = e.detail.elt;
    if (el.tagName !== 'A') return;
    if (shouldExcludeBoost(el)) el.setAttribute('hx-boost', 'false');
  });

  /* ─── Scroll helpers ─── */
  function headerOffset() {
    return (header && header.offsetHeight) || 80;
  }

  function scrollToHash(hash) {
    if (!hash || hash === '#') return false;
    const target = document.querySelector(hash);
    if (!target) return false;
    const top = target.getBoundingClientRect().top + window.scrollY - headerOffset() - 8;
    window.scrollTo({ top: Math.max(0, top), behavior: prefersReducedMotion ? 'auto' : 'smooth' });
    return true;
  }

  function scrollAfterNavigation() {
    if (scrollToHash(window.location.hash)) return;
    window.scrollTo({ top: 0, behavior: 'auto' });
  }

  /* ─── Page title from full HTML response ─── */
  function syncTitleFromResponse(xhr) {
    if (!xhr || !xhr.responseText) return;
    const match = xhr.responseText.match(/<title[^>]*>([^<]*)<\/title>/i);
    if (match && match[1]) document.title = match[1].trim();
  }

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
      const href = (link.getAttribute('href') || '').split('#')[0].replace(/\/$/, '') || '/';
      const active = path === href || (href !== '/' && path.startsWith(href));
      link.classList.toggle('nav-link-active', active);
    });
  }

  /* ─── Motorcycle detail finance calculator ─── */
  function initMotorcycleFinance(root) {
    const scope = root || document;
    const financeCfg = scope.querySelector('#motorcycle-finance-config');
    if (!financeCfg || !window.bootMotorcycleFinance) return;
    try {
      window.bootMotorcycleFinance(JSON.parse(financeCfg.textContent));
    } catch (_) {
      /* ignore */
    }
  }

  /* ─── Sticky header shrink ─── */
  function onScroll() {
    if (!header) return;
    header.classList.toggle('is-scrolled', window.scrollY > 16);
  }

  /* ─── Bảo dưỡng booking preselect from ?service=slug (client fallback) ─── */
  const SERVICE_SLUG_MAP = {
    'bao-duong-dinh-ky': 'Bảo dưỡng định kỳ',
    'thay-nhot-may': 'Thay nhớt máy',
    'thay-nhot-hop-so-xe-ga': 'Thay nhớt hộp số xe ga',
    'kiem-tra-loc-gio': 'Kiểm tra / thay lọc gió',
    'kiem-tra-bugi': 'Kiểm tra / thay bugi',
    'kiem-tra-phanh': 'Kiểm tra phanh / thay má phanh',
    'kiem-tra-lop': 'Kiểm tra lốp / vá lốp / thay lốp',
    'kiem-tra-dien-binh-ac-quy': 'Kiểm tra điện / bình ắc quy',
    'kiem-tra-dong-co': 'Kiểm tra động cơ',
    've-sinh-kim-phun-buong-dot': 'Vệ sinh kim phun / buồng đốt',
    'kiem-tra-day-curoa-noi-xe-ga': 'Kiểm tra dây curoa / nồi xe ga',
    'sua-chua-tong-quat': 'Sửa chữa tổng quát',
    'thay-phu-tung-chinh-hang': 'Thay phụ tùng chính hãng',
    'kiem-tra-xe-truoc-chuyen-di': 'Kiểm tra xe trước chuyến đi',
  };

  function initBookingFromQuery() {
    const slug = new URLSearchParams(window.location.search).get('service');
    if (!slug) return;
    const form = document.getElementById('booking');
    if (!form) return;
    const name = SERVICE_SLUG_MAP[slug];
    const serviceSelect = form.querySelector('[name="ServiceType"]');
    if (serviceSelect && name) serviceSelect.value = name;
  }

  /* ─── Main page initializer (idempotent) ─── */
  function initPage(root) {
    const scope = root && root.nodeType === 1 ? root : document;
    initImages(scope);
    initReveals(scope);
    updateNavActive();
    initMotorcycleFinance(scope);
    initBookingFromQuery();

    if (window.Alpine && scope !== document) {
      Alpine.initTree(scope);
    }

    if (scope === document || scope.id === 'main-content') {
      requestAnimationFrame(scrollAfterNavigation);
    }
  }

  window.HieuNgaApp = { initPage, scrollToHash, updateNavActive };

  /** Motorcycle detail gallery — must be global for Alpine after HTMX swap */
  window.detailGallery = function detailGallery(images) {
    return {
      images,
      active: 0,
      touchStartX: 0,
      go(i) {
        this.active = i;
      },
      next() {
        this.active = (this.active + 1) % this.images.length;
      },
      prev() {
        this.active = (this.active - 1 + this.images.length) % this.images.length;
      },
      onTouchStart(e) {
        this.touchStartX = e.changedTouches[0].screenX;
      },
      onTouchEnd(e) {
        const diff = e.changedTouches[0].screenX - this.touchStartX;
        if (Math.abs(diff) < 40) return;
        if (diff < 0) this.next();
        else this.prev();
      },
    };
  };

  /* ─── HTMX lifecycle ─── */
  document.body.addEventListener('htmx:beforeSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'catalog-grid' || target.id === 'blog-grid') {
      target.classList.add('is-loading');
    }
  });

  function onMainContentSwap(e) {
    const target = e.detail.target;
    if (target.id !== 'main-content') return;

    syncTitleFromResponse(e.detail.xhr);
    initPage(target);

    if (!prefersReducedMotion) {
      target.classList.add('page-enter');
      setTimeout(() => target.classList.remove('page-enter'), 320);
    }
  }

  document.body.addEventListener('htmx:afterSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'main-content') {
      onMainContentSwap(e);
    } else {
      initImages(target);
      initReveals(target);
    }
    if (target.id === 'catalog-grid' || target.id === 'blog-grid') {
      target.classList.remove('is-loading');
    }
  });

  document.body.addEventListener('htmx:afterSettle', (e) => {
    if (e.detail.target.id === 'main-content') {
      scrollAfterNavigation();
    }
    initImages(e.detail.target);
  });

  document.body.addEventListener('htmx:historyRestore', () => {
    const main = document.getElementById('main-content');
    if (main) initPage(main);
    updateNavActive();
    requestAnimationFrame(scrollAfterNavigation);
  });

  /* ─── Touch feedback (delegated, single listener) ─── */
  document.addEventListener(
    'touchstart',
    (e) => {
      const el = e.target.closest('.touch-press');
      if (el) el.classList.add('is-pressed');
    },
    { passive: true }
  );
  document.addEventListener(
    'touchend',
    (e) => {
      document.querySelectorAll('.touch-press.is-pressed').forEach((el) => el.classList.remove('is-pressed'));
    },
    { passive: true }
  );

  /* ─── Bảo dưỡng service booking preselect (delegated) ─── */
  document.addEventListener('click', (e) => {
    const link = e.target.closest('[data-book-service]');
    if (!link) return;

    const form = document.getElementById('booking');
    if (!form) return;

    const serviceSelect = form.querySelector('[name="ServiceType"]');
    const notesField = form.querySelector('[name="Notes"]');
    const option = link.dataset.bookService;
    const detail = link.dataset.serviceDetail;

    if (serviceSelect && option) serviceSelect.value = option;
    if (notesField && detail && !notesField.value.trim()) {
      notesField.value = 'Quan tâm dịch vụ: ' + detail;
    }
  });

  /* ─── Same-page hash anchors (no HTMX request) ─── */
  document.addEventListener('click', (e) => {
    const link = e.target.closest('a[href^="#"]');
    if (!link || link.getAttribute('hx-boost') === 'false') return;
    const hash = link.getAttribute('href');
    if (!hash || hash === '#') return;
    if (document.querySelector(hash)) {
      e.preventDefault();
      history.pushState(null, '', hash);
      scrollToHash(hash);
    }
  });

  /* ─── Motorcycle image error fallback ─── */
  const DEFAULT_MOTO_IMG = '/images/motorcycles/default.svg';

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

  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  document.addEventListener('DOMContentLoaded', () => initPage(document));
})();
