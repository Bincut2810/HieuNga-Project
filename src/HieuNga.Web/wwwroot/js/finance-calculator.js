/**
 * Public detail finance calculator — standalone ES module.
 * Flat formula mirrors HieuNga.Application.Finance.FinanceMath.Compute.
 */
const STEP = 500_000;
const FALLBACK_RATE = 0.0079; // FinanceMath.FallbackMonthlyRate

function formatVnd(n) {
  return new Intl.NumberFormat('vi-VN').format(Math.round(n || 0)) + ' ₫';
}

function parseVndInput(str) {
  const digits = String(str || '').replace(/[^\d]/g, '');
  return digits ? parseInt(digits, 10) : 0;
}

function roundStep(n) {
  return Math.round(Math.max(0, n) / STEP) * STEP;
}

/** Mirrors FinanceMath.Compute */
function compute(price, down, months, monthlyRate) {
  const principal = Math.max(0, price - down);
  const n = months;
  if (principal <= 0 || n <= 0) {
    return { monthly: 0, total: down, interest: 0, principal: 0 };
  }
  const monthly = Math.round(principal / n + principal * monthlyRate);
  const installmentTotal = monthly * n;
  const interest = Math.max(0, installmentTotal - principal);
  return { monthly, total: down + installmentTotal, interest, principal };
}

class FinanceCalculator {
  /** @param {HTMLElement} root */
  constructor(root) {
    this.root = root;
    this.price = Number(root.dataset.price || 0);
    this.downPaymentPercent = Number(root.dataset.downPercent || 20);
    this.termMonths = Number(root.dataset.termMonths || 12);
    this.banks = [];
    this.variants = [];
    try { this.banks = JSON.parse(root.dataset.banks || '[]'); } catch { this.banks = []; }
    try { this.variants = JSON.parse(root.dataset.variants || '[]'); } catch { this.variants = []; }
    this.selectedBankId = root.dataset.defaultBank || (this.banks[0] && this.banks[0].id) || '';
    this.downPayment = 0;
    this.result = { monthly: 0, total: 0, interest: 0, principal: 0 };
    this._bound = false;
  }

  get selectedBank() {
    return this.banks.find((b) => b.id === this.selectedBankId) || this.banks[0] || null;
  }

  get interestRate() {
    return this.selectedBank ? Number(this.selectedBank.rate) : FALLBACK_RATE;
  }

  get maxDown() {
    return Math.floor(this.price * 0.7);
  }

  initialize() {
    if (this.root.dataset.fcReady === '1') return;
    this.root.dataset.fcReady = '1';
    this.downPayment = roundStep((this.price * this.downPaymentPercent) / 100);
    if (this.downPayment > this.maxDown) this.downPayment = this.maxDown;
    this.bindEvents();
    this.calculate();
    this.render();
  }

  bindEvents() {
    if (this._bound) return;
    this._bound = true;

    this.root.querySelectorAll('[data-fc-variant]').forEach((btn) => {
      btn.addEventListener('click', () => {
        const price = Number(btn.dataset.price || 0);
        const id = btn.dataset.id || '';
        const variant = this.variants.find((v) => v.id === id);
        this.price = price > 0 ? price : this.price;
        this.downPayment = roundStep((this.price * this.downPaymentPercent) / 100);
        if (this.downPayment > this.maxDown) this.downPayment = this.maxDown;
        this.root.querySelectorAll('[data-fc-variant]').forEach((el) => {
          el.classList.toggle('border-honda-red', el === btn);
          el.classList.toggle('bg-red-50/60', el === btn);
          el.classList.toggle('border-gray-100', el !== btn);
        });
        const nameEl = this.root.querySelector('[data-fc-variant-name]');
        if (nameEl) {
          nameEl.hidden = !variant;
          nameEl.textContent = variant ? variant.name : '';
        }
        this.calculate();
        this.render();
      });
    });

    this.root.querySelectorAll('[data-fc-bank]').forEach((btn) => {
      btn.addEventListener('click', () => {
        this.selectedBankId = btn.dataset.id || '';
        this.calculate();
        this.render();
      });
    });

    this.root.querySelectorAll('[data-fc-down-pct]').forEach((btn) => {
      btn.addEventListener('click', () => {
        this.downPaymentPercent = Number(btn.dataset.pct || 20);
        this.downPayment = roundStep((this.price * this.downPaymentPercent) / 100);
        if (this.downPayment > this.maxDown) this.downPayment = this.maxDown;
        this.calculate();
        this.render();
      });
    });

    this.root.querySelectorAll('[data-fc-term]').forEach((btn) => {
      btn.addEventListener('click', () => {
        this.termMonths = Number(btn.dataset.months || 12);
        this.calculate();
        this.render();
      });
    });

    const slider = this.root.querySelector('[data-fc-down-slider]');
    if (slider) {
      slider.addEventListener('input', () => {
        this.downPayment = Number(slider.value || 0);
        this.downPaymentPercent = this.price > 0
          ? Math.min(70, Math.round((this.downPayment / this.price) * 100))
          : 0;
        this.calculate();
        this.render({ skipSlider: true });
      });
    }

    const input = this.root.querySelector('[data-fc-down-input]');
    if (input) {
      input.addEventListener('focus', () => {
        input.value = String(Math.round(this.downPayment));
      });
      const commit = () => {
        let val = roundStep(parseVndInput(input.value));
        if (val > this.maxDown) val = this.maxDown;
        this.downPayment = val;
        this.downPaymentPercent = this.price > 0
          ? Math.min(70, Math.round((this.downPayment / this.price) * 100))
          : 0;
        this.calculate();
        this.render();
      };
      input.addEventListener('blur', commit);
      input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') { e.preventDefault(); commit(); }
      });
    }

    const first = this.variants.find((v) => v.price > 0) || this.variants[0];
    if (first) {
      this.price = first.price > 0 ? first.price : this.price;
      this.downPayment = roundStep((this.price * this.downPaymentPercent) / 100);
      if (this.downPayment > this.maxDown) this.downPayment = this.maxDown;
      const btn = this.root.querySelector('[data-fc-variant][data-id="' + first.id + '"]');
      if (btn) {
        this.root.querySelectorAll('[data-fc-variant]').forEach((el) => {
          el.classList.toggle('border-honda-red', el === btn);
          el.classList.toggle('bg-red-50/60', el === btn);
          el.classList.toggle('border-gray-100', el !== btn);
        });
        const nameEl = this.root.querySelector('[data-fc-variant-name]');
        if (nameEl) {
          nameEl.hidden = false;
          nameEl.textContent = first.name || '';
        }
      }
    }
  }

  calculate() {
    this.result = compute(this.price, this.downPayment, this.termMonths, this.interestRate);
  }

  render(opts = {}) {
    const setText = (sel, text) => {
      const el = this.root.querySelector(sel);
      if (el) el.textContent = text;
    };

    setText('[data-fc-price]', formatVnd(this.price));
    setText('[data-fc-principal]', formatVnd(this.result.principal));
    setText('[data-fc-principal-stat]', formatVnd(this.result.principal));
    setText('[data-fc-down]', formatVnd(this.downPayment));
    setText('[data-fc-down-label]', formatVnd(this.downPayment));
    setText('[data-fc-monthly]', formatVnd(this.result.monthly));
    setText('[data-fc-total]', formatVnd(this.result.total));
    setText('[data-fc-interest]', formatVnd(this.result.interest));
    setText('[data-fc-term-label]', String(this.termMonths));

    const bank = this.selectedBank;
    setText('[data-fc-bank-name]', bank ? bank.name : '—');
    setText('[data-fc-bank-rate]', bank ? bank.rateLabel : '—');

    this.root.querySelectorAll('[data-fc-bank]').forEach((el) => {
      el.classList.toggle('is-selected', el.dataset.id === this.selectedBankId);
    });
    this.root.querySelectorAll('[data-fc-down-pct]').forEach((el) => {
      const on = Number(el.dataset.pct) === this.downPaymentPercent;
      el.classList.toggle('border-honda-red', on);
      el.classList.toggle('bg-honda-red', on);
      el.classList.toggle('text-white', on);
      el.classList.toggle('border-gray-200', !on);
    });
    this.root.querySelectorAll('[data-fc-term]').forEach((el) => {
      const on = Number(el.dataset.months) === this.termMonths;
      el.classList.toggle('border-honda-dark', on);
      el.classList.toggle('bg-honda-dark', on);
      el.classList.toggle('text-white', on);
      el.classList.toggle('border-gray-200', !on);
    });

    const slider = this.root.querySelector('[data-fc-down-slider]');
    if (slider && !opts.skipSlider) {
      slider.max = String(this.maxDown || 0);
      slider.value = String(this.downPayment);
    }

    const input = this.root.querySelector('[data-fc-down-input]');
    if (input && document.activeElement !== input) {
      input.value = formatVnd(this.downPayment);
    }
  }
}

const instances = new WeakMap();
let swapBound = false;

function findCalculators(scope) {
  if (!scope || !scope.querySelectorAll) return [];
  return Array.from(scope.querySelectorAll('[data-finance-calculator]'));
}

/** Idempotent: one instance per root element. No-op when none exist. */
export function initialize(root = document) {
  const nodes = findCalculators(root);
  if (nodes.length === 0) return;
  nodes.forEach((el) => {
    if (instances.has(el)) return;
    const calc = new FinanceCalculator(el);
    instances.set(el, calc);
    calc.initialize();
  });
}

function onContentSwap(e) {
  const target = e.detail && e.detail.target;
  if (!target || !target.querySelector) return;
  if (!target.querySelector('[data-finance-calculator]')) return;
  initialize(target);
}

function boot() {
  if (!document.querySelector('[data-finance-calculator]')) {
    // Still listen once so HTMX-navigated detail pages can mount without polish coupling.
    bindSwapOnce();
    return;
  }
  initialize(document);
  bindSwapOnce();
}

function bindSwapOnce() {
  if (swapBound || !document.body) return;
  swapBound = true;
  document.body.addEventListener('htmx:afterSwap', onContentSwap);
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', boot);
} else {
  boot();
}
