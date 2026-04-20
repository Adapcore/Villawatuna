// Leave edit form behaviors (half-day UI)
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    var durationSelect = document.getElementById('durationSelect');
    var halfDayRow = document.getElementById('halfDayRow');
    if (!durationSelect || !halfDayRow) return;

    function toggleHalfDay() {
      var isHalfDay = durationSelect.value === '2';
      halfDayRow.style.display = isHalfDay ? '' : 'none';
    }

    durationSelect.addEventListener('change', toggleHalfDay);
    toggleHalfDay();
  });
})();

