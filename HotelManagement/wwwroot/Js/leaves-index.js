// Leaves list page behaviors (filters auto-submit)
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('leaveFilterForm');
    if (!form) return;

    var rangeInput = document.getElementById('range');
    var fromDate = document.getElementById('fromDate');
    var toDate = document.getElementById('toDate');

    function submitIfReady() {
      form.submit();
    }

    ['employeeId', 'fromDate', 'toDate', 'status'].forEach(function (id) {
      var el = document.getElementById(id);
      if (!el) return;
      el.addEventListener('change', submitIfReady);
    });

    // Today / Month / Year buttons should keep Employee + Status selections.
    // Clear explicit From/To so the server-side range can apply like the old links did.
    document.querySelectorAll('[data-range]').forEach(function (btn) {
      btn.addEventListener('click', function (e) {
        e.preventDefault();
        var v = btn.getAttribute('data-range') || '';
        if (rangeInput) rangeInput.value = v;
        if (fromDate) fromDate.value = '';
        if (toDate) toDate.value = '';
        form.submit();
      });
    });
  });
})();

