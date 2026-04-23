// Leaves calendar filter checkbox navigation
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    function escapeHtml(text) {
      var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
      return (text || '').toString().replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    // Desktop-only: clicking a calendar day cell opens "Add Leave" for that date.
    // Mobile uses a different layout; we avoid changing tap behavior there.
    try {
      var isDesktop = window.matchMedia && window.matchMedia('(min-width: 768px)').matches;
      if (isDesktop) {
        document.addEventListener('click', function (e) {
          var target = e.target;
          if (!target) return;

          // Don't override existing links (leave badges -> Details etc.)
          if (target.closest && target.closest('a')) return;

          var cell = target.closest ? target.closest('td.calendar-day-cell[data-date]') : null;
          if (!cell) return;

          var date = cell.getAttribute('data-date') || '';
          if (!date) return;

          var params = new URLSearchParams();
          params.set('fromDate', date);
          params.set('toDate', date);
          var applyUrl = null;

          // If bootstrap modal isn't available, fall back to old behavior (navigate).
          if (!window.bootstrap || !bootstrap.Modal) {
            window.location.href = '/Leaves/Apply?' + params.toString();
            return;
          }

          var modalEl = document.getElementById('calendarDayModal');
          var modalTitleEl = document.getElementById('calendarDayModalLabel');
          var modalBodyEl = document.getElementById('calendarDayModalBody');
          var addBtn = document.getElementById('calendarDayAddBtn');
          if (!modalEl || !modalTitleEl || !modalBodyEl || !addBtn) {
            window.location.href = '/Leaves/Apply?' + params.toString();
            return;
          }

          var applyBase = modalEl.getAttribute('data-apply-url') || '/Leaves/Apply';
          var dayLeavesBase = modalEl.getAttribute('data-day-leaves-url') || '/Leaves/CalendarDayLeaves';
          applyUrl = applyBase + '?' + params.toString();

          addBtn.setAttribute('href', applyUrl);
          modalTitleEl.textContent = 'Leaves on ' + date;
          modalBodyEl.textContent = 'Loading…';

          var showOpenEl = document.getElementById('showOpen');
          var showApprovedEl = document.getElementById('showApproved');
          var showRejectedEl = document.getElementById('showRejected');

          var q = new URLSearchParams();
          q.set('date', date);
          if (showOpenEl) q.set('showOpen', showOpenEl.checked ? 'true' : 'false');
          if (showApprovedEl) q.set('showApproved', showApprovedEl.checked ? 'true' : 'false');
          if (showRejectedEl) q.set('showRejected', showRejectedEl.checked ? 'true' : 'false');

          var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
          modal.show();

          fetch(dayLeavesBase + '?' + q.toString(), {
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
          })
            .then(function (r) {
              // If auth/session expired you'll often get HTML here; surface something actionable.
              var ct = (r.headers && r.headers.get) ? (r.headers.get('content-type') || '') : '';
              if (!r.ok) {
                return r.text().then(function (t) {
                  throw new Error('Request failed (' + r.status + '). ' + (t || '').slice(0, 200));
                });
              }
              if (ct.indexOf('application/json') === -1) {
                return r.text().then(function (t) {
                  throw new Error('Unexpected response. ' + (t || '').slice(0, 200));
                });
              }
              return r.json();
            })
            .then(function (data) {
              if (!data || data.success !== true) {
                modalBodyEl.innerHTML = '<div class="alert alert-danger mb-0">Failed to load leaves for this day.</div>';
                return;
              }

              var leaves = data.leaves || [];
              if (leaves.length === 0) {
                modalBodyEl.innerHTML = '<div class="text-muted">No leaves for this day.</div>';
                return;
              }

              function statusBadgeClass(status) {
                if (status === 'Approved') return 'bg-success';
                if (status === 'Rejected') return 'bg-danger';
                if (status === 'Cancelled') return 'bg-secondary';
                if (status === 'Open') return 'bg-warning text-dark';
                return 'bg-secondary';
              }

              function statusItemClass(status) {
                // Bootstrap list-group contextual backgrounds (light tint)
                if (status === 'Approved') return 'list-group-item-success';
                if (status === 'Rejected') return 'list-group-item-danger';
                if (status === 'Open') return 'list-group-item-warning';
                return '';
              }

              var html = '<div class="list-group">';
              leaves.forEach(function (l) {
                var session = (l.halfDaySession || '').trim();
                var sessionText = session ? (' · ' + escapeHtml(session)) : '';
                var rangeText = escapeHtml(l.fromDate) + ' → ' + escapeHtml(l.toDate);
                var daysText = (l.noOfDays !== null && l.noOfDays !== undefined && String(l.noOfDays) !== '')
                  ? (' (' + escapeHtml(l.noOfDays) + ')')
                  : '';
                var leaveType = (l.leaveType || '').toString().trim();
                var typeHtml = leaveType ? ('<div class="small text-muted">Type: ' + escapeHtml(leaveType) + '</div>') : '';
                var reason = (l.reason || '').toString().trim();
                var reasonHtml = reason ? ('<div class="small text-muted mt-1">Reason: ' + escapeHtml(reason) + '</div>') : '';
                var itemClass = statusItemClass(l.status);

                html +=
                  '<a class="list-group-item list-group-item-action d-flex justify-content-between align-items-start gap-2 ' + itemClass + '" ' +
                  'href="/Leaves/Details/' + encodeURIComponent(l.id) + '">' +
                  '<div class="me-auto">' +
                  '<div class="fw-semibold">' + escapeHtml(l.employeeName) + '</div>' +
                  '<div class="text-muted small">' + rangeText + daysText + sessionText + '</div>' +
                  typeHtml +
                  reasonHtml +
                  '</div>' +
                  '<span class="badge ' + statusBadgeClass(l.status) + ' align-self-center">' + escapeHtml(l.status) + '</span>' +
                  '</a>';
              });
              html += '</div>';
              modalBodyEl.innerHTML = html;
            })
            .catch(function (err) {
              modalBodyEl.innerHTML =
                '<div class="alert alert-danger mb-0">' +
                '<div class="fw-semibold">Error loading leaves.</div>' +
                '<div class="small mt-1">' + escapeHtml((err && err.message) ? err.message : 'Please try again.') + '</div>' +
                '</div>';
            });
        });
      }
    } catch (_) {
      // Ignore calendar click enhancement if browser lacks support
    }

    var form = document.getElementById('calendarFilterForm');
    var openCb = document.getElementById('showOpen');
    var approvedCb = document.getElementById('showApproved');
    var rejectedCb = document.getElementById('showRejected');
    if (!form || !openCb || !approvedCb || !rejectedCb) return;

    var baseUrl = form.getAttribute('data-calendar-url') || '';
    if (!baseUrl) return;

    function navigateWithFilters() {
      var yearEl = form.querySelector('input[name="year"]');
      var monthEl = form.querySelector('input[name="month"]');

      var params = new URLSearchParams();
      if (yearEl && yearEl.value) params.set('year', yearEl.value);
      if (monthEl && monthEl.value) params.set('month', monthEl.value);

      params.set('showOpen', openCb.checked ? 'true' : 'false');
      params.set('showApproved', approvedCb.checked ? 'true' : 'false');
      params.set('showRejected', rejectedCb.checked ? 'true' : 'false');

      window.location.href = baseUrl + '?' + params.toString();
    }

    openCb.addEventListener('change', navigateWithFilters);
    approvedCb.addEventListener('change', navigateWithFilters);
    rejectedCb.addEventListener('change', navigateWithFilters);
  });
})();

