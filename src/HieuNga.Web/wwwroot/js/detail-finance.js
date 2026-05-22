/**
 * Motorcycle detail — unified Alpine store for variant price + installment calculator
 */
(function () {
  'use strict';

  const STEP = 500_000;

  const BANKS = [
    { id: 'mb', name: 'MB Bank', initials: 'MB', monthlyRate: 0.014, ratePercent: 1.4, rateLabel: '1,4%/tháng', trust: 'Ưu đãi HEAD', color: '#0054A6' },
    { id: 'tpb', name: 'TPBank', initials: 'TP', monthlyRate: 0.015, ratePercent: 1.5, rateLabel: '1,5%/tháng', trust: 'Duyệt nhanh', color: '#6B2C91' },
    { id: 'agri', name: 'Agribank', initials: 'AG', monthlyRate: 0.012, ratePercent: 1.2, rateLabel: '1,2%/tháng', trust: 'Lãi suất thấp', color: '#006B3F' },
    { id: 'tcb', name: 'Techcombank', initials: 'TC', monthlyRate: 0.016, ratePercent: 1.6, rateLabel: '1,6%/tháng', trust: 'Phổ biến', color: '#E30613' },
  ];

  function normId(id) {
    return String(id ?? '').toLowerCase();
  }

  function roundStep(n) {
    return Math.round(Math.max(0, n) / STEP) * STEP;
  }

  function formatVnd(n) {
    return new Intl.NumberFormat('vi-VN').format(Math.round(n)) + ' ₫';
  }

  function parseVndInput(str) {
    const digits = String(str).replace(/[^\d]/g, '');
    return digits ? parseInt(digits, 10) : 0;
  }

  /** Demo formula: gốc/kỳ + lãi trên dư nợ */
  function calculate(price, down, months, monthlyRate) {
    const principal = Math.max(0, price - down);
    const n = months;

    if (principal <= 0 || n <= 0) {
      return { monthly: 0, total: down, interest: 0, principal: 0 };
    }

    const monthly = Math.round(principal / n + principal * monthlyRate);
    const installmentTotal = monthly * n;
    const interest = Math.max(0, installmentTotal - principal);
    const total = down + installmentTotal;

    return { monthly, total, interest, principal };
  }

  function buildStore(config) {
    const variants = (config.variants || []).map((v) => ({
      ...v,
      id: normId(v.id),
      price: Number(v.price),
    }));
    const first = variants[0];
    const price = first ? first.price : Number(config.basePrice);
    const down = Number(config.down ?? roundStep(price * 0.2));

    return {
      bikeName: config.bikeName || '',
      motorcycleId: config.motorcycleId || '',
      variants,
      variantId: first ? first.id : null,
      variantName: first ? first.name : null,
      price,
      basePrice: Number(config.basePrice),
      banks: BANKS,
      bank: 'mb',
      down,
      months: 12,
      downPercent: price > 0 ? Math.min(70, Math.round((down / price) * 100)) : 20,
      downInput: formatVnd(down),
      monthlyAnim: false,
      _animTimer: null,
      result: { monthly: 0, total: 0, interest: 0, principal: 0 },

      get hasVariants() {
        return this.variants.length > 0;
      },

      get maxDown() {
        return Math.floor(this.price * 0.7);
      },

      get selectedBank() {
        return this.banks.find((b) => b.id === this.bank) || this.banks[0];
      },

      get monthly() {
        return this.result.monthly;
      },

      formatVnd,

      recalculate() {
        const bank = this.banks.find((b) => b.id === this.bank) || this.banks[0];
        this.result = calculate(this.price, this.down, this.months, bank.monthlyRate);
      },

      init() {
        this.refreshDownInput();
        this.recalculate();
        this.broadcast(false);
      },

      broadcast(animate) {
        if (animate !== false) this.triggerAnim();
        window.dispatchEvent(
          new CustomEvent('finance-updated', {
            detail: { monthly: this.result.monthly, price: this.price },
          })
        );
      },

      triggerAnim() {
        this.monthlyAnim = true;
        clearTimeout(this._animTimer);
        this._animTimer = setTimeout(() => {
          this.monthlyAnim = false;
        }, 420);
      },

      pulse() {
        this.recalculate();
        this.broadcast(true);
      },

      selectVariant(id) {
        const v = this.variants.find((x) => x.id === normId(id));
        if (!v) return;
        this.variantId = v.id;
        this.variantName = v.name;
        this.applyPrice(v.price);
      },

      applyPrice(newPrice) {
        this.price = Number(newPrice);
        if (this.down > this.maxDown) this.down = this.maxDown;
        if (this.downPercent > 0) {
          this.down = roundStep((this.price * this.downPercent) / 100);
          if (this.down > this.maxDown) this.down = this.maxDown;
        }
        this.refreshDownInput();
        this.syncDownPercent();
        this.pulse();
      },

      selectBank(id) {
        this.bank = id;
        this.pulse();
      },

      setTerm(months) {
        this.months = months;
        this.pulse();
      },

      setDownPercent(pct) {
        this.downPercent = pct;
        this.down = roundStep((this.price * pct) / 100);
        if (this.down > this.maxDown) this.down = this.maxDown;
        this.refreshDownInput();
        this.pulse();
      },

      onDownSlider() {
        this.syncDownPercent();
        this.refreshDownInput();
        this.pulse();
      },

      syncDownPercent() {
        if (this.price <= 0) {
          this.downPercent = 0;
          return;
        }
        this.downPercent = Math.min(70, Math.round((this.down / this.price) * 100));
      },

      refreshDownInput() {
        this.downInput = formatVnd(this.down);
      },

      onDownInputFocus() {
        this.downInput = String(Math.round(this.down));
      },

      commitDownInput() {
        let val = parseVndInput(this.downInput);
        val = roundStep(val);
        if (val > this.maxDown) val = this.maxDown;
        if (val > this.price) val = roundStep(this.price);
        this.down = val;
        this.syncDownPercent();
        this.refreshDownInput();
        this.pulse();
      },

      isVariantSelected(id) {
        return this.variantId === normId(id);
      },

      isBankSelected(id) {
        return this.bank === id;
      },

      isTermSelected(months) {
        return this.months === months;
      },

      isDownPercentSelected(pct) {
        return this.downPercent === pct;
      },
    };
  }

  function registerStore(config) {
    const store = buildStore(config);
    if (typeof Alpine !== 'undefined' && typeof Alpine.store === 'function') {
      Alpine.store('motorcycleDetail', store);
      store.init();
      return store;
    }
    document.addEventListener(
      'alpine:init',
      () => {
        Alpine.store('motorcycleDetail', store);
        store.init();
      },
      { once: true }
    );
    return store;
  }

  window.registerMotorcycleFinance = registerStore;

  /** Boot from inline config (full page + HTMX) */
  window.bootMotorcycleFinance = function (config) {
    return registerStore(config);
  };

})();
