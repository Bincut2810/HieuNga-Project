/**
 * Honda Hiếu Nga — global UX polish (HTMX, header, images, motion)
 * Sprint 4.1 — no GSAP; IntersectionObserver + CSS only
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
    scope.querySelectorAll('.reveal:not(.is-visible)').forEach((el) => {
      revealObserver.observe(el);
      // Immediately reveal in-viewport nodes (HTMX swaps often miss the first IO tick)
      const rect = el.getBoundingClientRect();
      const vh = window.innerHeight || 0;
      if (rect.bottom > 0 && rect.top < vh) {
        el.classList.add('is-visible');
        revealObserver.unobserve(el);
      }
    });
  }

  /* ─── Stagger: ensure direct children of [data-stagger] get .reveal ─── */
  function initStagger(root) {
    const scope = root || document;
    scope.querySelectorAll('[data-stagger]').forEach((group) => {
      Array.from(group.children).forEach((child) => {
        if (!child.classList.contains('reveal')) child.classList.add('reveal');
      });
    });
  }

  /* ─── Light parallax ─── */
  let parallaxNodes = [];
  let parallaxRaf = 0;

  function collectParallax(root) {
    const scope = root || document;
    const found = Array.from(scope.querySelectorAll('[data-parallax]'));
    if (scope === document) {
      parallaxNodes = found;
    } else {
      const set = new Set(parallaxNodes.concat(found));
      parallaxNodes = Array.from(set);
    }
  }

  function tickParallax() {
    parallaxRaf = 0;
    if (prefersReducedMotion || !parallaxNodes.length) return;
    const vh = window.innerHeight || 1;
    parallaxNodes.forEach((el) => {
      if (!el.isConnected) return;
      const speed = parseFloat(el.getAttribute('data-parallax') || '0.12');
      const rect = el.getBoundingClientRect();
      const mid = rect.top + rect.height / 2;
      const offset = ((mid - vh / 2) / vh) * -40 * speed;
      el.style.transform = 'translate3d(0,' + offset.toFixed(2) + 'px,0)';
    });
  }

  function onParallaxScroll() {
    if (parallaxRaf || prefersReducedMotion) return;
    parallaxRaf = requestAnimationFrame(tickParallax);
  }

  /* ─── Counters ─── */
  let counterObserver;

  function animateCounter(el) {
    if (el.dataset.counted === '1') return;
    el.dataset.counted = '1';
    const target = parseFloat(el.getAttribute('data-counter') || '0');
    const suffix = el.getAttribute('data-counter-suffix') || '';
    const prefix = el.getAttribute('data-counter-prefix') || '';
    const decimals = parseInt(el.getAttribute('data-counter-decimals') || '0', 10);
    if (prefersReducedMotion || !Number.isFinite(target)) {
      el.textContent = prefix + target + suffix;
      return;
    }
    const duration = 1100;
    const start = performance.now();
    function frame(now) {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      const value = target * eased;
      el.textContent =
        prefix +
        (decimals > 0 ? value.toFixed(decimals) : Math.round(value).toLocaleString('vi-VN')) +
        suffix;
      if (t < 1) requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  }

  function initCounters(root) {
    const scope = root || document;
    const nodes = scope.querySelectorAll('[data-counter]:not([data-counted="1"])');
    if (!nodes.length) return;
    if (prefersReducedMotion) {
      nodes.forEach(animateCounter);
      return;
    }
    if (!counterObserver) {
      counterObserver = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              animateCounter(entry.target);
              counterObserver.unobserve(entry.target);
            }
          });
        },
        { threshold: 0.35 }
      );
    }
    nodes.forEach((el) => counterObserver.observe(el));
  }

  /* ─── Button ripple ─── */
  function setRipplePoint(btn, clientX, clientY) {
    const rect = btn.getBoundingClientRect();
    const x = ((clientX - rect.left) / rect.width) * 100;
    const y = ((clientY - rect.top) / rect.height) * 100;
    btn.style.setProperty('--rx', x + '%');
    btn.style.setProperty('--ry', y + '%');
    btn.classList.remove('is-rippling');
    void btn.offsetWidth;
    btn.classList.add('is-rippling');
    window.setTimeout(() => btn.classList.remove('is-rippling'), 420);
  }

  document.addEventListener(
    'pointerdown',
    (e) => {
      if (prefersReducedMotion || e.button !== 0) return;
      const btn = e.target.closest('.btn-primary, .btn-ripple');
      if (!btn || btn.disabled || btn.classList.contains('is-disabled') || btn.classList.contains('is-loading')) return;
      setRipplePoint(btn, e.clientX, e.clientY);
    },
    { passive: true }
  );

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
    onParallaxScroll();
  }

  /* ─── Bảo dưỡng booking preselect from ?service=slug (client fallback) ─── */
  const SERVICE_SLUG_MAP = {
    'sua-chua-thay-the-phu-tung': 'Sửa chữa & thay thế phụ tùng',
    'bao-hanh-bao-duong': 'Bảo hành & bảo dưỡng',
    'dau-nhot-chinh-hang': 'Dầu nhớt chính hãng',
    'sua-chua-luu-dong': 'Sửa chữa lưu động',
    'bao-hiem-xe-may': 'Bảo hiểm xe máy',
    'tan-trang-cham-soc-xe': 'Tân trang & chăm sóc xe',
    'bao-duong-dinh-ky': 'Bảo hành & bảo dưỡng',
    'thay-nhot-may': 'Dầu nhớt chính hãng',
    'thay-nhot-hop-so-xe-ga': 'Dầu nhớt chính hãng',
    'sua-chua-tong-quat': 'Sửa chữa & thay thế phụ tùng',
    'thay-phu-tung-chinh-hang': 'Sửa chữa & thay thế phụ tùng',
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
    initStagger(scope);
    initImages(scope);
    initReveals(scope);
    collectParallax(scope);
    initCounters(scope);
    updateNavActive();
    initMotorcycleFinance(scope);
    initBookingFromQuery();
    tickParallax();

    if (!scope.querySelector || !scope.querySelector('.detail-page')) {
      document.body.classList.remove('detail-sticky-visible');
    }

    if (window.registerMotorcycleDetailUi) {
      window.registerMotorcycleDetailUi();
    }

    if (window.Alpine && scope !== document) {
      Alpine.initTree(scope);
    }

    if (scope === document || scope.id === 'main-content') {
      requestAnimationFrame(scrollAfterNavigation);
    }
  }

  window.HieuNgaApp = { initPage, scrollToHash, updateNavActive };

  /* ─── HTMX lifecycle ─── */
  document.body.addEventListener('htmx:beforeSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'catalog-browse' || target.id === 'catalog-grid' || target.id === 'blog-grid') {
      target.classList.add('is-loading');
    }
  });

  function onMainContentSwap(e) {
    const target = e.detail.target;
    if (target.id !== 'main-content') return;

    syncTitleFromResponse(e.detail.xhr);
    initPage(target);
    bootDetailPageModules(target);

    if (!prefersReducedMotion) {
      target.classList.add('page-enter');
      setTimeout(() => target.classList.remove('page-enter'), 320);
    }
  }

  /** HTMX boost strips/ignores inline scripts in swapped HTML — re-boot detail modules. */
  function loadScriptOnce(src) {
    return new Promise((resolve, reject) => {
      if (src.indexOf('detail-finance') >= 0 && typeof window.bootMotorcycleFinance === 'function') {
        resolve();
        return;
      }
      if (src.indexOf('detail-viewer') >= 0 && typeof window.registerMotorcycleDetailUi === 'function') {
        resolve();
        return;
      }
      if (document.querySelector('script[data-hn-src="' + src + '"]')) {
        resolve();
        return;
      }
      const s = document.createElement('script');
      s.src = src;
      s.dataset.hnSrc = src;
      s.async = false;
      s.onload = () => resolve();
      s.onerror = () => reject(new Error('Failed to load ' + src));
      document.body.appendChild(s);
    });
  }

  /** Boot finance after full load AND HTMX boost/history. */
  function bootDetailPageModules(root) {
    const scope = root || document;
    const financeCfg = scope.querySelector('#motorcycle-finance-config');
    const needsViewer = !!scope.querySelector('.detail-page');

    const tasks = [];
    if (financeCfg) tasks.push(loadScriptOnce('/js/detail-finance.js'));
    if (needsViewer) tasks.push(loadScriptOnce('/js/detail-viewer.js'));

    const runBoot = () => {
      if (financeCfg && typeof window.bootMotorcycleFinance === 'function') {
        try {
          window.bootMotorcycleFinance(JSON.parse(financeCfg.textContent || '{}'));
        } catch (err) {
          console.warn('Finance init:', err);
        }
      }
      if (needsViewer && typeof window.registerMotorcycleDetailUi === 'function') {
        try { window.registerMotorcycleDetailUi(); } catch (_) { /* already registered */ }
      }
      if (needsViewer && typeof Alpine !== 'undefined' && typeof Alpine.initTree === 'function') {
        try { Alpine.initTree(scope); } catch (err) { console.warn('Alpine initTree:', err); }
      }
    };

    if (!tasks.length) {
      runBoot();
      return;
    }

    Promise.all(tasks).then(runBoot).catch((err) => console.warn('Detail module load:', err));
  }

  document.body.addEventListener('htmx:afterSwap', (e) => {
    const target = e.detail.target;
    if (target.id === 'main-content') {
      onMainContentSwap(e);
    } else {
      initStagger(target);
      initImages(target);
      initReveals(target);
      collectParallax(target);
      initCounters(target);
    }
    if (target.id === 'catalog-browse' || target.id === 'catalog-grid' || target.id === 'blog-grid') {
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
    if (main) {
      initPage(main);
      bootDetailPageModules(main);
    }
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
    () => {
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
  window.addEventListener('resize', onParallaxScroll, { passive: true });
  onScroll();

  document.addEventListener('DOMContentLoaded', () => initPage(document));
})();
