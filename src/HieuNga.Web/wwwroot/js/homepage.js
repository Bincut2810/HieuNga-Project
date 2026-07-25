/**
 * Homepage flagship — hero carousel, promo rail, review slider.
 * No animation libraries. Keyboard + swipe + pause on hover / hidden tab.
 */
(function () {
  'use strict';

  function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  function initHomeHero(root) {
    const hero = (root || document).querySelector('[data-home-hero]');
    if (!hero || hero.dataset.ready === '1') return;
    const slides = Array.from(hero.querySelectorAll('[data-hero-slide]'));
    if (slides.length <= 1) {
      hero.dataset.ready = '1';
      return;
    }

    hero.dataset.ready = '1';
    const dotsWrap = hero.querySelector('[data-hero-dots]');
    const prevBtn = hero.querySelector('[data-hero-prev]');
    const nextBtn = hero.querySelector('[data-hero-next]');
    const progress = hero.querySelector('[data-hero-progress]');
    const live = hero.querySelector('[data-hero-live]');
    const intervalMs = Math.max(4000, parseInt(hero.dataset.interval || '6000', 10) || 6000);
    const reduceMotion = prefersReducedMotion();
    let index = 0;
    let timer = null;
    let paused = false;
    let progressRaf = null;
    let progressStart = 0;

    slides.forEach((slide, i) => {
      if (dotsWrap) {
        const dot = document.createElement('button');
        dot.type = 'button';
        dot.className = 'home-hero-dot' + (i === 0 ? ' is-active' : '');
        dot.setAttribute('role', 'tab');
        dot.setAttribute('aria-label', 'Banner ' + (i + 1));
        dot.setAttribute('aria-controls', slide.id || ('home-hero-slide-' + i));
        dot.setAttribute('aria-selected', i === 0 ? 'true' : 'false');
        dot.tabIndex = i === 0 ? 0 : -1;
        dot.addEventListener('click', () => go(i, true));
        dotsWrap.appendChild(dot);
      }
    });

    function setProgress(pct) {
      if (!progress) return;
      progress.style.transform = 'scaleX(' + Math.max(0, Math.min(1, pct)) + ')';
    }

    function stopProgress() {
      if (progressRaf) cancelAnimationFrame(progressRaf);
      progressRaf = null;
    }

    function runProgress() {
      stopProgress();
      if (reduceMotion || paused || !progress || document.hidden) {
        setProgress(0);
        return;
      }
      progressStart = performance.now();
      setProgress(0);
      function frame(now) {
        if (paused || document.hidden) return;
        const t = (now - progressStart) / intervalMs;
        setProgress(t);
        if (t < 1) progressRaf = requestAnimationFrame(frame);
      }
      progressRaf = requestAnimationFrame(frame);
    }

    function announce() {
      if (live) live.textContent = 'Banner ' + (index + 1) + ' / ' + slides.length;
    }

    function go(next, user) {
      index = (next + slides.length) % slides.length;
      slides.forEach((slide, i) => {
        const on = i === index;
        slide.classList.toggle('is-active', on);
        slide.setAttribute('aria-hidden', on ? 'false' : 'true');
      });
      if (dotsWrap) {
        dotsWrap.querySelectorAll('.home-hero-dot').forEach((dot, i) => {
          const on = i === index;
          dot.classList.toggle('is-active', on);
          dot.setAttribute('aria-selected', on ? 'true' : 'false');
          dot.tabIndex = on ? 0 : -1;
        });
      }
      announce();
      if (user) restart();
      else runProgress();
    }

    function tick() {
      if (!paused && !document.hidden) go(index + 1, false);
    }

    function stopTimer() {
      clearInterval(timer);
      timer = null;
      stopProgress();
    }

    function restart() {
      stopTimer();
      if (reduceMotion) {
        setProgress(0);
        return;
      }
      if (paused || document.hidden) return;
      runProgress();
      timer = setInterval(tick, intervalMs);
    }

    prevBtn && prevBtn.addEventListener('click', () => go(index - 1, true));
    nextBtn && nextBtn.addEventListener('click', () => go(index + 1, true));

    hero.addEventListener('mouseenter', () => {
      paused = true;
      stopProgress();
    });
    hero.addEventListener('mouseleave', () => {
      paused = false;
      restart();
    });
    hero.addEventListener('focusin', () => {
      paused = true;
      stopProgress();
    });
    hero.addEventListener('focusout', (e) => {
      if (!hero.contains(e.relatedTarget)) {
        paused = false;
        restart();
      }
    });

    hero.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowLeft') { e.preventDefault(); go(index - 1, true); }
      else if (e.key === 'ArrowRight') { e.preventDefault(); go(index + 1, true); }
      else if (e.key === 'Home') { e.preventDefault(); go(0, true); }
      else if (e.key === 'End') { e.preventDefault(); go(slides.length - 1, true); }
    });

    document.addEventListener('visibilitychange', () => {
      if (document.hidden) stopTimer();
      else if (!paused) restart();
    });

    let touchX = null;
    let touchY = null;
    hero.addEventListener('touchstart', (e) => {
      const t = e.changedTouches[0];
      touchX = t?.clientX ?? null;
      touchY = t?.clientY ?? null;
    }, { passive: true });
    hero.addEventListener('touchend', (e) => {
      if (touchX == null) return;
      const t = e.changedTouches[0];
      const dx = (t?.clientX ?? touchX) - touchX;
      const dy = (t?.clientY ?? touchY ?? 0) - (touchY ?? 0);
      if (Math.abs(dx) > 40 && Math.abs(dx) > Math.abs(dy)) {
        go(index + (dx < 0 ? 1 : -1), true);
      }
      touchX = null;
      touchY = null;
    }, { passive: true });

    restart();
  }

  function initPromoRail(root) {
    const rail = (root || document).querySelector('[data-promo-rail]');
    if (!rail || rail.dataset.ready === '1') return;
    rail.dataset.ready = '1';
    const prev = (root || document).querySelector('[data-promo-prev]');
    const next = (root || document).querySelector('[data-promo-next]');
    const step = () => Math.min(rail.clientWidth * 0.85, 420);

    prev && prev.addEventListener('click', () => {
      rail.scrollBy({ left: -step(), behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
    });
    next && next.addEventListener('click', () => {
      rail.scrollBy({ left: step(), behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
    });

    rail.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowLeft') { e.preventDefault(); rail.scrollBy({ left: -step(), behavior: 'smooth' }); }
      if (e.key === 'ArrowRight') { e.preventDefault(); rail.scrollBy({ left: step(), behavior: 'smooth' }); }
    });

    (root || document).querySelectorAll('[data-countdown]').forEach((el) => {
      const end = Date.parse(el.getAttribute('data-countdown') || '');
      if (!end) return;
      const days = Math.max(0, Math.ceil((end - Date.now()) / 86400000));
      el.textContent = days <= 0 ? 'Sắp kết thúc' : 'Còn ' + days + ' ngày';
    });
  }

  function initReviewSlider(root) {
    const slider = (root || document).querySelector('[data-review-slider]');
    if (!slider || slider.dataset.ready === '1') return;
    const slides = Array.from(slider.querySelectorAll('[data-review-slide]'));
    if (slides.length === 0) return;
    slider.dataset.ready = '1';

    const track = slider.querySelector('[data-review-track]');
    const dotsWrap = slider.querySelector('[data-review-dots]');
    const prevBtn = (root || document).querySelector('[data-review-prev]');
    const nextBtn = (root || document).querySelector('[data-review-next]');
    const intervalMs = Math.max(4000, parseInt(slider.dataset.interval || '5500', 10) || 5500);
    const reduceMotion = prefersReducedMotion();
    let index = 0;
    let timer = null;
    let paused = false;

    function pages() {
      return slides.length;
    }

    slides.forEach((_, i) => {
      if (!dotsWrap || slides.length <= 1) return;
      const dot = document.createElement('button');
      dot.type = 'button';
      dot.className = 'home-review-dot' + (i === 0 ? ' is-active' : '');
      dot.setAttribute('aria-label', 'Đánh giá ' + (i + 1));
      dot.addEventListener('click', () => go(i, true));
      dotsWrap.appendChild(dot);
    });

    function go(next, user) {
      index = (next + pages()) % pages();
      if (track) {
        track.style.transform = 'translateX(-' + index * 100 + '%)';
      }
      slides.forEach((slide, i) => {
        slide.setAttribute('aria-hidden', i === index ? 'false' : 'true');
      });
      if (dotsWrap) {
        dotsWrap.querySelectorAll('.home-review-dot').forEach((dot, i) => {
          dot.classList.toggle('is-active', i === index);
        });
      }
      if (user) restart();
    }

    function tick() {
      if (!paused) go(index + 1, false);
    }

    function restart() {
      clearInterval(timer);
      timer = null;
      if (reduceMotion || slides.length <= 1) return;
      timer = setInterval(tick, intervalMs);
    }

    prevBtn && prevBtn.addEventListener('click', () => go(index - 1, true));
    nextBtn && nextBtn.addEventListener('click', () => go(index + 1, true));

    slider.addEventListener('mouseenter', () => { paused = true; });
    slider.addEventListener('mouseleave', () => { paused = false; });
    slider.addEventListener('focusin', () => { paused = true; });
    slider.addEventListener('focusout', (e) => {
      if (!slider.contains(e.relatedTarget)) paused = false;
    });
    slider.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowLeft') { e.preventDefault(); go(index - 1, true); }
      if (e.key === 'ArrowRight') { e.preventDefault(); go(index + 1, true); }
    });

    let touchX = null;
    slider.addEventListener('touchstart', (e) => {
      touchX = e.changedTouches[0]?.clientX ?? null;
    }, { passive: true });
    slider.addEventListener('touchend', (e) => {
      if (touchX == null) return;
      const dx = (e.changedTouches[0]?.clientX ?? touchX) - touchX;
      if (Math.abs(dx) > 40) go(index + (dx < 0 ? 1 : -1), true);
      touchX = null;
    }, { passive: true });

    go(0, false);
    restart();
  }

  function boot(root) {
    initHomeHero(root);
    initPromoRail(root);
    initReviewSlider(root);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => boot(document));
  } else {
    boot(document);
  }

  document.body.addEventListener('htmx:afterSwap', (e) => {
    if (e.detail.target && e.detail.target.id === 'main-content') {
      boot(e.detail.target);
    }
  });

  window.HieuNgaHome = { init: boot };
})();
