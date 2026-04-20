// Leaves calendar filter checkbox navigation
(function () {
  document.addEventListener('DOMContentLoaded', function () {
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

