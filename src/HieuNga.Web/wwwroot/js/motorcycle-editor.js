/**
 * Motorcycle CMS Editor — dirty tracking, slug auto, sticky save UX.
 */
(function () {
  'use strict';

  function toSlug(text) {
    return text
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .replace(/đ/g, 'd')
      .replace(/[^a-z0-9\s-]/g, '')
      .trim()
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');
  }

  function initSlug() {
    var nameInput = document.getElementById('moto-name');
    var slugInput = document.getElementById('moto-slug');
    if (!nameInput || !slugInput) return;
    var slugTouched = slugInput.value.length > 0;
    slugInput.addEventListener('input', function () {
      slugTouched = true;
    });
    nameInput.addEventListener('input', function () {
      if (!slugTouched) slugInput.value = toSlug(nameInput.value);
    });
  }

  function initDirtyForms() {
    document.querySelectorAll('[data-dirty-form]').forEach(function (form) {
      var dirty = false;
      var hint = form.querySelector('[data-dirty-hint]');
      function markDirty() {
        dirty = true;
        if (hint) hint.hidden = false;
        form.classList.add('is-dirty');
      }
      form.addEventListener('input', markDirty);
      form.addEventListener('change', markDirty);
      form.addEventListener('submit', function () {
        dirty = false;
      });
      window.addEventListener('beforeunload', function (e) {
        if (!dirty) return;
        e.preventDefault();
        e.returnValue = '';
      });
      var cancel = form.querySelector('[data-cancel-link]');
      if (cancel) {
        cancel.addEventListener('click', function (e) {
          if (dirty && !confirm('Có thay đổi chưa lưu. Rời trang?')) e.preventDefault();
        });
      }
    });

    document.querySelectorAll('.admin-editor-tab.is-disabled').forEach(function (tab) {
      tab.addEventListener('click', function (e) {
        e.preventDefault();
        alert('Lưu xe ở tab General trước khi dùng tab này.');
      });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initSlug();
    initDirtyForms();
  });
})();
