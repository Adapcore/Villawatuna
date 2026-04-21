// Leave apply form behaviors (days preview, half-day UI, date sync)
(function () {
  function parseIsoDate(value) {
    if (!value) return null;
    var parts = value.split('-');
    if (parts.length !== 3) return null;
    var y = parseInt(parts[0], 10);
    var m = parseInt(parts[1], 10) - 1;
    var d = parseInt(parts[2], 10);
    return new Date(y, m, d);
  }

  function inclusiveDays(from, to) {
    if (!from || !to) return null;
    var n = 0;
    var cur = new Date(from.getFullYear(), from.getMonth(), from.getDate());
    var end = new Date(to.getFullYear(), to.getMonth(), to.getDate());
    while (cur <= end) {
      n++;
      cur.setDate(cur.getDate() + 1);
    }
    return n;
  }

  function formatDays(v) {
    if (v === null) return '—';
    return Number.isInteger(v) ? String(v) : String(v);
  }

  document.addEventListener('DOMContentLoaded', function () {
    var durationSelect = document.getElementById('durationSelect');
    var halfDayRow = document.getElementById('halfDayRow');
    var lastDayHalfRow = document.getElementById('lastDayHalfRow');
    var lastDayHalfCheck = document.getElementById('lastDayHalfCheck');
    var fromDateInput = document.getElementById('fromDateInput');
    var toDateInput = document.getElementById('toDateInput');
    var noOfDaysDisplay = document.getElementById('noOfDaysDisplay');
    var halfDaySessionLabel = document.getElementById('halfDaySessionLabel');

    if (!durationSelect && !fromDateInput && !toDateInput) return;

    function ensureToNotBeforeFrom() {
      if (!fromDateInput || !toDateInput || !fromDateInput.value) return;
      if (!toDateInput.value || toDateInput.value < fromDateInput.value) {
        toDateInput.value = fromDateInput.value;
      }
    }

    function computeNoOfDays() {
      var from = parseIsoDate(fromDateInput && fromDateInput.value);
      var to = parseIsoDate(toDateInput && toDateInput.value);
      var isHalfDuration = durationSelect && durationSelect.value === '2';
      var lastHalf = lastDayHalfCheck && lastDayHalfCheck.checked;

      if (!from || !to) return null;
      if (from > to) return null;

      if (isHalfDuration) {
        return from.getTime() === to.getTime() ? 0.5 : null;
      }

      var inc = inclusiveDays(from, to);
      if (inc === null) return null;

      if (lastHalf) {
        if (from.getTime() >= to.getTime()) return null;
        return inc - 0.5;
      }

      return inc;
    }

    function refreshNoOfDays() {
      if (!noOfDaysDisplay) return;
      var v = computeNoOfDays();
      noOfDaysDisplay.value = formatDays(v);
    }

    function toggleHalfDayUi() {
      ensureToNotBeforeFrom();

      var isHalfDay = durationSelect && durationSelect.value === '2';
      if (toDateInput) {
        toDateInput.disabled = !!isHalfDay;
      }
      if (isHalfDay && fromDateInput && toDateInput && fromDateInput.value) {
        if (!toDateInput.value || toDateInput.value !== fromDateInput.value) {
          toDateInput.value = fromDateInput.value;
        }
      }

      var from = parseIsoDate(fromDateInput && fromDateInput.value);
      var to = parseIsoDate(toDateInput && toDateInput.value);
      var rangeOk = from && to && from < to;
      var isFullDay = durationSelect && durationSelect.value === '1';

      if (lastDayHalfRow) {
        lastDayHalfRow.style.display = isFullDay && rangeOk ? '' : 'none';
      }
      if ((!isFullDay || !rangeOk) && lastDayHalfCheck) {
        lastDayHalfCheck.checked = false;
      }

      var lastHalf = lastDayHalfCheck && lastDayHalfCheck.checked;
      var showSession = isHalfDay || (isFullDay && lastHalf && rangeOk);

      if (halfDayRow) halfDayRow.style.display = showSession ? '' : 'none';
      if (halfDaySessionLabel) {
        if (isHalfDay) halfDaySessionLabel.textContent = 'Half day session';
        else if (isFullDay && lastHalf) halfDaySessionLabel.textContent = 'Half of last day (To date)';
        else halfDaySessionLabel.textContent = 'Half day session';
      }

      refreshNoOfDays();
    }

    if (durationSelect) durationSelect.addEventListener('change', toggleHalfDayUi);
    if (fromDateInput) {
      fromDateInput.addEventListener('change', toggleHalfDayUi);
      fromDateInput.addEventListener('input', toggleHalfDayUi);
    }
    if (toDateInput) toDateInput.addEventListener('change', toggleHalfDayUi);
    if (lastDayHalfCheck) lastDayHalfCheck.addEventListener('change', toggleHalfDayUi);

    toggleHalfDayUi();
  });
})();

