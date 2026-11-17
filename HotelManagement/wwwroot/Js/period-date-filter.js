/**
 * Period date filter plugin.
 *
 * Markup lives inside one root element and uses the pf-* classes.
 * Another page can copy that markup and call:
 *
 * $('#myFilter').periodDateFilter({
 *   defaultMode: 'range',
 *   onChange: function (state) {
 *     // state.mode, state.fromDate, state.toDate (yyyy-MM-dd or null)
 *   }
 * });
 *
 * var filter = $('#myFilter').data('periodDateFilter');
 * filter.selectMode('daily');
 */
(function ($) {
  var monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  var shortMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  var instanceCount = 0;

  function PeriodDateFilter(root, options) {
    this.$root = $(root);
    this.options = $.extend({
      defaultMode: 'range',
      onChange: function () {}
    }, options || {});
    this.mode = this.options.defaultMode;
    this.anchor = null;
    this.fromDate = null;
    this.toDate = null;
    this.isInternal = false;
    this.yearPageStart = 0;
    this.dayView = null;
    this.monthView = null;
    this.ns = '.periodDateFilter' + (++instanceCount);
    this.bind();
    this.selectMode(this.mode);
  }

  PeriodDateFilter.prototype.$ = function (selector) {
    return this.$root.find(selector);
  };

  PeriodDateFilter.prototype.bind = function () {
    var self = this;

    this.$root.on('click', '[data-range]', function (e) {
      e.preventDefault();
      self.selectMode($(this).data('range') || 'daily');
    });

    this.$('.pf-prev').on('click', function (e) {
      e.preventDefault();
      self.shift(-1);
    });

    this.$('.pf-next').on('click', function (e) {
      e.preventDefault();
      self.shift(1);
    });

    this.$('.pf-from, .pf-to').on('change', function () {
      if (self.isInternal) return;
      self.fromDate = self.$('.pf-from').val() || null;
      self.toDate = self.$('.pf-to').val() || null;
      self.notify();
    });

    this.$('.pf-day-display').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.$('.pf-year-panel, .pf-month-panel').addClass('d-none');
      self.toggleDayPanel();
    });

    this.$('.pf-day-month-prev').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.dayView = new Date(self.dayView.getFullYear(), self.dayView.getMonth() - 1, 1);
      self.renderDayGrid();
    });

    this.$('.pf-day-month-next').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.dayView = new Date(self.dayView.getFullYear(), self.dayView.getMonth() + 1, 1);
      self.renderDayGrid();
    });

    this.$root.on('click', '.pf-day-option', function (e) {
      e.preventDefault();
      var day = parseInt($(this).data('day'), 10);
      if (!day || !self.dayView) return;
      var picked = new Date(self.dayView.getFullYear(), self.dayView.getMonth(), day);
      self.anchor = self.mode === 'weekly' ? startOfWeek(picked) : picked;
      self.$('.pf-day-panel').addClass('d-none');
      self.applyPeriod();
    });

    this.$('.pf-day-today').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.anchor = startOfToday();
      self.$('.pf-day-panel').addClass('d-none');
      self.applyPeriod();
    });

    this.$('.pf-month-display').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.$('.pf-day-panel, .pf-year-panel').addClass('d-none');
      self.toggleMonthPanel();
    });

    this.$('.pf-month-year-prev').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.monthView = new Date(self.monthView.getFullYear() - 1, self.monthView.getMonth(), 1);
      self.renderMonthGrid();
    });

    this.$('.pf-month-year-next').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.monthView = new Date(self.monthView.getFullYear() + 1, self.monthView.getMonth(), 1);
      self.renderMonthGrid();
    });

    this.$root.on('click', '.pf-month-option', function (e) {
      e.preventDefault();
      var year = parseInt($(this).data('year'), 10);
      var month = parseInt($(this).data('month'), 10);
      if (!year || isNaN(month)) return;
      self.anchor = new Date(year, month, 1);
      self.$('.pf-month-panel').addClass('d-none');
      self.applyPeriod();
    });

    this.$('.pf-year-display').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.$('.pf-day-panel, .pf-month-panel').addClass('d-none');
      self.toggleYearPanel();
    });

    this.$('.pf-year-page-prev').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.yearPageStart -= 12;
      self.renderYearGrid();
    });

    this.$('.pf-year-page-next').on('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      self.yearPageStart += 12;
      self.renderYearGrid();
    });

    this.$root.on('click', '.pf-year-option', function (e) {
      e.preventDefault();
      var year = parseInt($(this).data('year'), 10);
      if (!year) return;
      self.anchor = new Date(year, 0, 1);
      self.$('.pf-year-panel').addClass('d-none');
      self.applyPeriod();
    });

    $(document).on('click' + this.ns, function (e) {
      if (!self.containsPicker('.pf-year-picker', e.target)) self.$('.pf-year-panel').addClass('d-none');
      if (!self.containsPicker('.pf-day-picker', e.target)) self.$('.pf-day-panel').addClass('d-none');
      if (!self.containsPicker('.pf-month-picker', e.target)) self.$('.pf-month-panel').addClass('d-none');
    });
  };

  PeriodDateFilter.prototype.containsPicker = function (selector, target) {
    var $picker = this.$(selector);
    return $picker.is(target) || $picker.has(target).length > 0;
  };

  PeriodDateFilter.prototype.notify = function () {
    this.options.onChange({
      mode: this.mode,
      fromDate: this.fromDate,
      toDate: this.toDate
    });
  };

  PeriodDateFilter.prototype.selectMode = function (mode) {
    this.mode = mode;
    this.$('[data-range]').each(function () {
      var $button = $(this);
      var isActive = ($button.data('range') || '') === mode;
      $button.toggleClass('btn-primary', isActive);
      $button.toggleClass('btn-outline-secondary', !isActive);
    });

    if (mode === 'range') {
      this.$('.pf-period').addClass('d-none');
      this.$('.pf-from-wrap, .pf-to-wrap').removeClass('d-none');
      this.setDates(null, null);
      this.notify();
      return;
    }

    this.$('.pf-period').removeClass('d-none');
    this.$('.pf-from-wrap, .pf-to-wrap').addClass('d-none');
    this.anchor = startOfToday();
    this.applyPeriod();
  };

  PeriodDateFilter.prototype.shift = function (direction) {
    if (!this.anchor || this.mode === 'range') return;
    var anchor = this.anchor;

    if (this.mode === 'daily') {
      this.anchor = addDays(anchor, direction);
    } else if (this.mode === 'weekly') {
      this.anchor = addDays(startOfWeek(anchor), direction * 7);
    } else if (this.mode === 'monthly') {
      this.anchor = new Date(anchor.getFullYear(), anchor.getMonth() + direction, 1);
    } else if (this.mode === 'yearly') {
      this.anchor = new Date(anchor.getFullYear() + direction, 0, 1);
    }

    this.applyPeriod();
  };

  PeriodDateFilter.prototype.applyPeriod = function () {
    var bounds = periodBounds(this.mode, this.anchor);
    if (!bounds) return;
    this.setDates(formatDateLocal(bounds.from), formatDateLocal(bounds.to));
    this.updateUi();
    this.notify();
  };

  PeriodDateFilter.prototype.setDates = function (fromDate, toDate) {
    this.isInternal = true;
    this.$('.pf-from').val(fromDate || '');
    this.$('.pf-to').val(toDate || '');
    this.isInternal = false;
    this.fromDate = fromDate || null;
    this.toDate = toDate || null;
  };

  PeriodDateFilter.prototype.updateUi = function () {
    var mode = this.mode;
    var anchor = this.anchor;
    var labels = { daily: 'Date', weekly: 'Week', monthly: 'Month', yearly: 'Year' };
    var showDayPicker = mode === 'daily' || mode === 'weekly';

    this.$('.pf-period').toggleClass('pf-period-weekly', mode === 'weekly');
    this.$('.pf-field-label').text(labels[mode] || 'Date');
    this.$('.pf-day-display .pf-value').text(periodLabel(mode === 'weekly' ? 'weekly' : 'daily', anchor));
    this.$('.pf-day-picker').toggleClass('d-none', !showDayPicker);
    if (!showDayPicker) {
      this.$('.pf-day-panel').addClass('d-none');
    } else if (!this.$('.pf-day-panel').hasClass('d-none')) {
      this.dayView = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
      this.renderDayGrid();
    }

    this.$('.pf-month-display .pf-value').text(periodLabel('monthly', anchor));
    this.$('.pf-month-picker').toggleClass('d-none', mode !== 'monthly');
    if (mode !== 'monthly') {
      this.$('.pf-month-panel').addClass('d-none');
    } else if (!this.$('.pf-month-panel').hasClass('d-none')) {
      this.monthView = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
      this.renderMonthGrid();
    }

    this.$('.pf-year-display .pf-value').text(periodLabel('yearly', anchor));
    this.$('.pf-year-picker').toggleClass('d-none', mode !== 'yearly');
    if (mode !== 'yearly') {
      this.$('.pf-year-panel').addClass('d-none');
    }
  };

  PeriodDateFilter.prototype.toggleDayPanel = function () {
    var $panel = this.$('.pf-day-panel');
    if ($panel.hasClass('d-none')) {
      this.dayView = new Date(this.anchor.getFullYear(), this.anchor.getMonth(), 1);
      this.renderDayGrid();
      $panel.removeClass('d-none');
      return;
    }
    $panel.addClass('d-none');
  };

  PeriodDateFilter.prototype.renderDayGrid = function () {
    var view = this.dayView;
    var year = view.getFullYear();
    var month = view.getMonth();
    var selected = this.anchor;
    var firstWeekday = new Date(year, month, 1).getDay();
    var daysInMonth = new Date(year, month + 1, 0).getDate();
    var today = startOfToday();
    var weekStart = this.mode === 'weekly' ? startOfWeek(selected) : null;
    var weekEnd = weekStart ? addDays(weekStart, 6) : null;
    var html = '';
    var i;
    var day;

    this.$('.pf-day-month-label').text(monthNames[month] + ' ' + year);
    for (i = 0; i < firstWeekday; i++) html += '<span class="pf-day-pad"></span>';
    for (day = 1; day <= daysInMonth; day++) {
      var cell = new Date(year, month, day);
      var isSelected = weekStart
        ? cell.getTime() >= weekStart.getTime() && cell.getTime() <= weekEnd.getTime()
        : selected.getFullYear() === year && selected.getMonth() === month && selected.getDate() === day;
      var isToday = today.getFullYear() === year && today.getMonth() === month && today.getDate() === day;
      var classes = 'pf-day-option';
      if (isSelected) classes += ' is-selected';
      if (isToday) classes += ' is-today';
      html += '<button type="button" class="' + classes + '" data-day="' + day + '">' + day + '</button>';
    }
    this.$('.pf-day-grid').html(html);
  };

  PeriodDateFilter.prototype.toggleMonthPanel = function () {
    var $panel = this.$('.pf-month-panel');
    if ($panel.hasClass('d-none')) {
      this.monthView = new Date(this.anchor.getFullYear(), this.anchor.getMonth(), 1);
      this.renderMonthGrid();
      $panel.removeClass('d-none');
      return;
    }
    $panel.addClass('d-none');
  };

  PeriodDateFilter.prototype.renderMonthGrid = function () {
    var center = this.monthView;
    var selected = this.anchor;
    var first = new Date(center.getFullYear(), center.getMonth() - 4, 1);
    var last = new Date(center.getFullYear(), center.getMonth() + 4, 1);
    var html = '';
    var offset;

    this.$('.pf-month-year-label').text(first.getFullYear() === last.getFullYear()
      ? String(center.getFullYear())
      : first.getFullYear() + ' – ' + last.getFullYear());

    for (offset = -4; offset <= 4; offset++) {
      var monthDate = new Date(center.getFullYear(), center.getMonth() + offset, 1);
      var isSelected = selected.getFullYear() === monthDate.getFullYear() && selected.getMonth() === monthDate.getMonth();
      html += '<button type="button" class="pf-month-option' + (isSelected ? ' is-selected' : '') + '" data-year="' + monthDate.getFullYear() + '" data-month="' + monthDate.getMonth() + '">';
      html += shortMonths[monthDate.getMonth()];
      html += '<span>' + monthDate.getFullYear() + '</span>';
      html += '</button>';
    }
    this.$('.pf-month-grid').html(html);
  };

  PeriodDateFilter.prototype.toggleYearPanel = function () {
    var $panel = this.$('.pf-year-panel');
    if ($panel.hasClass('d-none')) {
      var year = this.anchor.getFullYear();
      this.yearPageStart = year - (year % 12);
      this.renderYearGrid();
      $panel.removeClass('d-none');
      return;
    }
    $panel.addClass('d-none');
  };

  PeriodDateFilter.prototype.renderYearGrid = function () {
    var selected = this.anchor.getFullYear();
    var start = this.yearPageStart;
    var html = '';
    var year;
    for (year = start; year < start + 12; year++) {
      html += '<button type="button" class="pf-year-option' + (year === selected ? ' is-selected' : '') + '" data-year="' + year + '">' + year + '</button>';
    }
    this.$('.pf-year-grid').html(html);
    this.$('.pf-year-page-label').text(start + ' – ' + (start + 11));
  };

  function startOfToday() {
    var now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), now.getDate());
  }

  function startOfWeek(date) {
    var d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() - d.getDay());
    return d;
  }

  function addDays(date, days) {
    var d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() + days);
    return d;
  }

  function sameDay(a, b) {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }

  function formatDateLocal(date) {
    return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0');
  }

  function formatDisplayDate(date) {
    return date.getDate() + ' ' + shortMonths[date.getMonth()] + ' ' + date.getFullYear();
  }

  function periodBounds(mode, anchor) {
    if (mode === 'daily') return { from: anchor, to: anchor };
    if (mode === 'weekly') {
      var start = startOfWeek(anchor);
      return { from: start, to: addDays(start, 6) };
    }
    if (mode === 'monthly') {
      return {
        from: new Date(anchor.getFullYear(), anchor.getMonth(), 1),
        to: new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0)
      };
    }
    if (mode === 'yearly') {
      return {
        from: new Date(anchor.getFullYear(), 0, 1),
        to: new Date(anchor.getFullYear(), 11, 31)
      };
    }
    return null;
  }

  function periodLabel(mode, anchor) {
    var today = startOfToday();
    if (mode === 'daily') {
      if (sameDay(anchor, today)) return 'Today';
      if (sameDay(anchor, addDays(today, -1))) return 'Yesterday';
      return formatDisplayDate(anchor);
    }
    if (mode === 'weekly') {
      var start = startOfWeek(anchor);
      var thisWeek = startOfWeek(today);
      if (sameDay(start, thisWeek)) return 'This Week';
      if (sameDay(start, addDays(thisWeek, -7))) return 'Last Week';
      return formatDisplayDate(start) + ' – ' + formatDisplayDate(addDays(start, 6));
    }
    if (mode === 'monthly') {
      if (anchor.getFullYear() === today.getFullYear() && anchor.getMonth() === today.getMonth()) return 'This Month';
      var lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      if (anchor.getFullYear() === lastMonth.getFullYear() && anchor.getMonth() === lastMonth.getMonth()) return 'Last Month';
      return monthNames[anchor.getMonth()] + ' ' + anchor.getFullYear();
    }
    if (mode === 'yearly') {
      if (anchor.getFullYear() === today.getFullYear()) return 'This Year';
      if (anchor.getFullYear() === today.getFullYear() - 1) return 'Last Year';
      return String(anchor.getFullYear());
    }
    return '';
  }

  $.fn.periodDateFilter = function (options) {
    return this.each(function () {
      if (!$.data(this, 'periodDateFilter')) {
        $.data(this, 'periodDateFilter', new PeriodDateFilter(this, options));
      }
    });
  };
})(jQuery);
