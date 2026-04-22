// Employees list search (server-rendered paging)
(function () {
  function buildUrlWithQuery(q) {
    var url = new URL(window.location.href);
    if (q && q.trim().length >= 3) {
      url.searchParams.set('q', q.trim());
      url.searchParams.set('page', '1');
    } else {
      url.searchParams.delete('q');
      url.searchParams.set('page', '1');
    }
    return url.toString();
  }

  document.addEventListener('DOMContentLoaded', function () {
    var input = document.getElementById('employeeSearch');
    if (!input) return;

    var timer = null;
    input.addEventListener('input', function () {
      var v = input.value || '';
      if (timer) window.clearTimeout(timer);

      timer = window.setTimeout(function () {
        var trimmed = v.trim();
        if (trimmed.length === 0 || trimmed.length >= 3) {
          window.location.href = buildUrlWithQuery(trimmed);
        }
      }, 350);
    });
  });
})();

