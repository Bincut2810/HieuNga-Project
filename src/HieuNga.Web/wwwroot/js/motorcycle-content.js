/**
 * Motorcycle Content Builder — specs / SEO / finance preview / editor chrome (Sprint 2.3).
 */
(function () {
  'use strict';

  function qs(s, r) { return (r || document).querySelector(s); }
  function qsa(s, r) { return Array.from((r || document).querySelectorAll(s)); }

  /* ── Spec builder ── */
  function initSpecBuilder() {
    var form = qs('[data-spec-builder]');
    if (!form) return;
    var rowsEl = qs('[data-spec-rows]', form);
    var sync = qs('#specs-lines-sync', form);
    var preview = qs('[data-spec-preview]', form);
    var initial = (sync && sync.value) || '';

    function parse(text) {
      return text.split(/\r?\n/).filter(Boolean).map(function (line) {
        if (line.indexOf('##') === 0) return { type: 'group', key: line.replace(/^##\s*/, ''), value: '' };
        var p = line.split('|');
        return { type: 'row', key: (p[0] || '').trim(), value: (p[1] || '').trim() };
      });
    }

    function serialize() {
      return qsa('[data-spec-row]', rowsEl).map(function (row) {
        var type = row.getAttribute('data-type');
        var key = qs('[data-spec-key]', row).value.trim();
        var val = qs('[data-spec-val]', row);
        if (type === 'group') return '## ' + key;
        return key + '|' + (val ? val.value.trim() : '');
      }).filter(function (l) { return l !== '|' && l !== '##'; }).join('\n');
    }

    function renderPreview() {
      if (!preview) return;
      preview.innerHTML = '';
      parse(serialize()).forEach(function (item) {
        if (item.type === 'group') {
          var g = document.createElement('dt');
          g.className = 'spec-preview-group';
          g.textContent = item.key;
          preview.appendChild(g);
          return;
        }
        var dt = document.createElement('dt');
        dt.textContent = item.key;
        var dd = document.createElement('dd');
        dd.textContent = item.value;
        preview.appendChild(dt);
        preview.appendChild(dd);
      });
    }

    function addRow(data) {
      data = data || { type: 'row', key: '', value: '' };
      var row = document.createElement('div');
      row.className = 'spec-row' + (data.type === 'group' ? ' is-group' : '');
      row.setAttribute('data-spec-row', '');
      row.setAttribute('data-type', data.type);
      row.draggable = true;
      row.innerHTML =
        '<span class="mm-card-handle">⋮⋮</span>' +
        (data.type === 'group'
          ? '<input class="admin-input" data-spec-key placeholder="Group name" value="' + escapeAttr(data.key) + '" />' +
            '<input type="hidden" data-spec-val value="" />'
          : '<input class="admin-input" data-spec-key placeholder="Key" value="' + escapeAttr(data.key) + '" />' +
            '<input class="admin-input" data-spec-val placeholder="Value" value="' + escapeAttr(data.value) + '" />') +
        '<div class="spec-row-actions">' +
        '<button type="button" class="admin-action admin-action-edit" data-spec-dup>Dup</button>' +
        '<button type="button" class="admin-action admin-action-danger" data-spec-del>Del</button>' +
        '</div>';
      rowsEl.appendChild(row);
      bindRow(row);
      syncOut();
    }

    function escapeAttr(s) {
      return String(s || '').replace(/"/g, '&quot;');
    }

    function bindRow(row) {
      row.addEventListener('input', syncOut);
      qs('[data-spec-del]', row).addEventListener('click', function () {
        row.remove();
        syncOut();
      });
      qs('[data-spec-dup]', row).addEventListener('click', function () {
        addRow({
          type: row.getAttribute('data-type'),
          key: qs('[data-spec-key]', row).value,
          value: qs('[data-spec-val]', row).value
        });
      });
      row.addEventListener('dragstart', function () { row.classList.add('is-dragging'); });
      row.addEventListener('dragend', function () { row.classList.remove('is-dragging'); syncOut(); });
      row.addEventListener('dragover', function (e) {
        e.preventDefault();
        var dragging = qs('.spec-row.is-dragging', rowsEl);
        if (!dragging || dragging === row) return;
        var rect = row.getBoundingClientRect();
        var before = (e.clientY - rect.top) < rect.height / 2;
        rowsEl.insertBefore(dragging, before ? row : row.nextSibling);
      });
    }

    function syncOut() {
      if (sync) sync.value = serialize();
      renderPreview();
      form.classList.add('is-dirty');
      var badge = qs('[data-unsaved-badge]');
      if (badge) badge.hidden = false;
    }

    parse(initial).forEach(addRow);
    if (!initial) addRow({ type: 'row', key: '', value: '' });

    qs('[data-spec-add-row]', form).addEventListener('click', function () { addRow({ type: 'row', key: '', value: '' }); });
    qs('[data-spec-add-group]', form).addEventListener('click', function () { addRow({ type: 'group', key: 'Nhóm mới', value: '' }); });
    form.addEventListener('submit', function () { if (sync) sync.value = serialize(); });
  }

  /* ── Content card reorder ── */
  function initContentSortable() {
    qsa('[data-sortable-content]').forEach(function (grid) {
      var kind = grid.getAttribute('data-sortable-content');
      var dragEl = null;
      qsa('[draggable=true]', grid).forEach(function (card) {
        card.addEventListener('dragstart', function () { dragEl = card; card.classList.add('is-dragging'); });
        card.addEventListener('dragend', function () {
          card.classList.remove('is-dragging');
          dragEl = null;
          var ids = qsa('[data-id]', grid).map(function (el) { return el.getAttribute('data-id'); });
          var input = qs('[data-content-order="' + kind + '"]');
          var btn = qs('[data-content-save-order="' + kind + '"]');
          if (input) input.value = ids.join(',');
          if (btn) btn.hidden = false;
        });
        card.addEventListener('dragover', function (e) {
          e.preventDefault();
          if (!dragEl || dragEl === card) return;
          var rect = card.getBoundingClientRect();
          var before = (e.clientY - rect.top) < rect.height / 2;
          grid.insertBefore(dragEl, before ? card : card.nextSibling);
        });
      });
    });
  }

  /* ── SEO live ── */
  function initSeo() {
    var root = qs('[data-seo-builder]');
    if (!root) return;
    var title = qs('[data-seo-title]', root);
    var desc = qs('[data-seo-desc]', root);
    var og = qs('[data-seo-og]', root);
    var ogImg = qs('[data-seo-og-img]', root);
    var serpTitle = qs('[data-seo-serp-title]', root);
    var serpDesc = qs('[data-seo-serp-desc]', root);
    var warnings = qs('[data-seo-warnings]', root);
    var fallbackTitle = (serpTitle && serpTitle.textContent) || '';

    function refresh() {
      var t = (title && title.value) || fallbackTitle;
      var d = (desc && desc.value) || '';
      var tc = qs('[data-seo-count=title]', root);
      var dc = qs('[data-seo-count=desc]', root);
      if (tc) {
        tc.textContent = t.length + '/60';
        tc.classList.toggle('is-warn', t.length > 60 || t.length < 10);
      }
      if (dc) {
        dc.textContent = d.length + '/160';
        dc.classList.toggle('is-warn', d.length > 160 || (d.length > 0 && d.length < 50));
      }
      if (serpTitle) serpTitle.textContent = t || 'Meta title';
      if (serpDesc) serpDesc.textContent = d || 'Meta description sẽ hiện ở đây…';
      if (ogImg && og) {
        var url = og.value.trim();
        if (url) { ogImg.src = url; ogImg.style.display = ''; }
      }
      if (warnings) {
        var msgs = [];
        if (!t) msgs.push('Thiếu Title');
        if (!d) msgs.push('Thiếu Description');
        if (og && !og.value.trim()) msgs.push('Thiếu OG Image (dùng thumbnail nếu trống)');
        warnings.innerHTML = msgs.length
          ? '<div class="admin-flash admin-flash-error">' + msgs.join(' · ') + '</div>'
          : '';
      }
    }
    [title, desc, og].forEach(function (el) {
      if (el) el.addEventListener('input', refresh);
    });
    refresh();
  }

  /* ── Editor chrome: sticky tabs, Ctrl+S, unsaved, autosave UI ── */
  function initEditorChrome() {
    var tabs = qs('.admin-editor-tabs');
    if (tabs) tabs.classList.add('is-sticky');

    var header = qs('.admin-page-header');
    if (header && !qs('[data-unsaved-badge]')) {
      var badge = document.createElement('span');
      badge.className = 'admin-badge admin-badge-category';
      badge.setAttribute('data-unsaved-badge', '');
      badge.hidden = true;
      badge.textContent = 'Unsaved';
      header.appendChild(badge);
      var auto = document.createElement('span');
      auto.className = 'admin-hint';
      auto.style.marginLeft = '0.5rem';
      auto.setAttribute('data-autosave-ui', '');
      auto.textContent = 'Autosave: off (UI)';
      header.appendChild(auto);
    }

    document.addEventListener('input', function (e) {
      if (!e.target.closest('[data-dirty-form], [data-spec-builder], [data-seo-builder]')) return;
      var badge = qs('[data-unsaved-badge]');
      if (badge) badge.hidden = false;
    });

    document.addEventListener('keydown', function (e) {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
        e.preventDefault();
        var form = qs('form[data-dirty-form].is-dirty, form[data-dirty-form], form[data-spec-builder], form[data-seo-builder], #moto-editor-form, #moto-quick-create');
        if (!form) return;
        var auto = qs('[data-autosave-ui]');
        if (auto) auto.textContent = 'Saving…';
        form.requestSubmit ? form.requestSubmit() : form.submit();
      }
    });

    qsa('form[data-dirty-form], form[data-spec-builder]').forEach(function (form) {
      form.addEventListener('submit', function () {
        var badge = qs('[data-unsaved-badge]');
        if (badge) badge.hidden = true;
        var auto = qs('[data-autosave-ui]');
        if (auto) auto.textContent = 'Saved just now (UI)';
      });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initSpecBuilder();
    initContentSortable();
    initSeo();
    initEditorChrome();
  });
})();
