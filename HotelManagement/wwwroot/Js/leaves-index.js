// Leaves list page behaviors (filters auto-submit)
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('leaveFilterForm');
    if (!form) return;

    function submitIfReady() {
      form.submit();
    }

    ['employeeId', 'fromDate', 'toDate', 'status'].forEach(function (id) {
      var el = document.getElementById(id);
      if (!el) return;
      el.addEventListener('change', submitIfReady);
    });
  });
})();

