/**
 * Leaves Index Page JavaScript
 * AJAX-based loading, filters, pagination, and approve/reject
 */

var leaveState = {
  currentPage: 1,
  employeeId: null,
  status: null,
  fromDate: null,
  toDate: null,
  range: null,
  isInternalCallScope: false,
};

$(document).ready(function () {
  // Filter changes (auto refresh)
  $('#employeeId, #status').on('change', function () {
    leaveState.employeeId = $('#employeeId').val() || null;
    leaveState.status = $('#status').val() || null;
    leaveState.currentPage = 1;
    loadLeaves();
  });

  $('#fromDate, #toDate').on('change', function () {
    if (!leaveState.isInternalCallScope) {
      leaveState.fromDate = $('#fromDate').val() || null;
      leaveState.toDate = $('#toDate').val() || null;
      // Manual date change clears quick-range button highlight
      clearRangeButtonActive();
      leaveState.currentPage = 1;
      loadLeaves();
    }
  });

  function formatDateLocal(date) {
    var year = date.getFullYear();
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var day = String(date.getDate()).padStart(2, '0');
    return year + '-' + month + '-' + day;
  }

  function setDatesInternal(fromDate, toDate) {
    leaveState.isInternalCallScope = true;
    $('#fromDate').val(fromDate || '');
    $('#toDate').val(toDate || '');
    leaveState.isInternalCallScope = false;
    leaveState.fromDate = fromDate || null;
    leaveState.toDate = toDate || null;
  }

  function setRangeButtonActive(rangeValue) {
    // Match invoice feel: one selected at a time.
    $('[data-range]').each(function () {
      var $b = $(this);
      var isActive = ($b.data('range') || '') === rangeValue;
      $b.toggleClass('btn-secondary', isActive);
      $b.toggleClass('btn-outline-secondary', !isActive);
    });
  }

  function clearRangeButtonActive() {
    $('[data-range]').each(function () {
      $(this).removeClass('btn-secondary').addClass('btn-outline-secondary');
    });
  }

  // Today / Month / Year buttons: keep Employee + Status; set explicit From/To like Invoice
  $(document).on('click', '[data-range]', function (e) {
    e.preventDefault();
    var v = $(this).data('range') || '';

    var now = new Date();
    now = new Date(now.getFullYear(), now.getMonth(), now.getDate());

    if (v === 'today') {
      var t = formatDateLocal(now);
      setDatesInternal(t, t);
    } else if (v === 'month') {
      var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
      var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
      setDatesInternal(formatDateLocal(firstDay), formatDateLocal(lastDay));
    } else if (v === 'year') {
      var fy = new Date(now.getFullYear(), 0, 1);
      var ly = new Date(now.getFullYear(), 11, 31);
      setDatesInternal(formatDateLocal(fy), formatDateLocal(ly));
    } else {
      setDatesInternal(null, null);
    }

    // Keep range for server consistency (and initial render), but also set explicit dates.
    $('#range').val(v);
    leaveState.range = v;
    setRangeButtonActive(v);
    leaveState.currentPage = 1;
    loadLeaves();
  });

  // Pagination clicks (delegated)
  $(document).on('click', '#leavePaginationContainer .pagination .page-link', function (e) {
    e.preventDefault();
    var href = $(this).attr('href');
    if (href && href !== '#') {
      var pageMatch = href.match(/page=(\d+)/);
      if (pageMatch) {
        leaveState.currentPage = parseInt(pageMatch[1]);
        loadLeaves();
      }
    }
  });

  // Highlight clicked action buttons (View/Edit/etc)
  $(document).on('click', '.leave-action-btn', function () {
    highlightClickedActionButton($(this));
  });

  // Approve/Reject (delegated)
  $(document).on('click', '.js-leave-approve-btn, .js-leave-reject-btn', function (e) {
    e.preventDefault();
    e.stopPropagation();

    if (typeof showConfirmDialog !== 'function') return;

    var btn = $(this);
    highlightClickedActionButton(btn);
    var id = btn.data('leave-id');
    var isApprove = btn.hasClass('js-leave-approve-btn');

    var message = isApprove
      ? 'Do you want to approve Leave #' + id + '?'
      : 'Do you want to reject Leave #' + id + '?';

    var options = isApprove
      ? { type: 'Success', title: 'Approve Leave', yesButtonText: 'Yes, Approve', noButtonText: 'No', dialogId: 'approveLeaveDialog_' + id }
      : { type: 'Danger', title: 'Reject Leave', yesButtonText: 'Yes, Reject', noButtonText: 'No', warningText: 'This action cannot be undone.', dialogId: 'rejectLeaveDialog_' + id };

    showConfirmDialog(message, function (confirmed) {
      if (!confirmed) {
        btn.removeClass('active');
        return;
      }
      postLeaveDecision(isApprove ? 'Approve' : 'Reject', id, btn);
    }, options);

    return false;
  });

  // Initial load (use current filter values already on the page)
  leaveState.employeeId = $('#employeeId').val() || null;
  leaveState.status = $('#status').val() || null;
  leaveState.fromDate = $('#fromDate').val() || null;
  leaveState.toDate = $('#toDate').val() || null;
  leaveState.range = $('#range').val() || null;

  if (leaveState.range) setRangeButtonActive(leaveState.range);

  loadLeaves();
});

function highlightClickedActionButton($btn) {
  if (!$btn || !$btn.length) return;
  // Remove active from sibling action buttons within same action group
  var $group = $btn.closest('.leave-action-group');
  if ($group.length) {
    $group.find('.leave-action-btn').removeClass('active');
  }
  $btn.addClass('active');
}

function loadLeaves() {
  // Show loading indicator
  $('#leaveLoadingIndicator').show();
  $('#leaveTableDesktop').hide();
  $('#leaveTableMobile').hide();
  $('#leavePaginationContainer').hide();

  // Build request data
  var requestData = {
    page: leaveState.currentPage,
    employeeId: leaveState.employeeId,
    status: leaveState.status,
    fromDate: leaveState.fromDate,
    toDate: leaveState.toDate,
    range: leaveState.range,
  };

  $.ajax({
    url: '/Leaves/GetLeaves',
    type: 'GET',
    data: requestData,
    success: function (response) {
      if (response && response.success) {
        renderLeaveTotals(response.totals);
        renderLeaves(response.leaves || [], response.isAdmin === true);
        renderLeavePagination(response.pagination);
      } else {
        showLeaveError('Failed to load leaves.');
      }
    },
    error: function () {
      showLeaveError('Error loading leaves. Please try again.');
    },
    complete: function () {
      $('#leaveLoadingIndicator').hide();
      $('#leaveTableDesktop').show();
      $('#leaveTableMobile').show();
      $('#leavePaginationContainer').show();
    }
  });
}

function renderLeaveTotals(totals) {
  totals = totals || { totalDays: 0, openDays: 0, approvedDays: 0, rejectedDays: 0 };
  function fmt(v) {
    var n = Number(v || 0);
    return n.toFixed(1);
  }

  var desktop =
    '<div class="d-none d-md-flex justify-content-end fw-bold">' +
    'Total Days: <span class="text-primary ms-1">' + fmt(totals.totalDays) + '</span>' +
    '<span class="mx-2 text-muted">|</span>' +
    'Open Days: <span class="text-primary ms-1">' + fmt(totals.openDays) + '</span>' +
    '<span class="mx-2 text-muted">|</span>' +
    'Approved Days: <span class="text-primary ms-1">' + fmt(totals.approvedDays) + '</span>' +
    '<span class="mx-2 text-muted">|</span>' +
    'Rejected Days: <span class="text-primary ms-1">' + fmt(totals.rejectedDays) + '</span>' +
    '</div>';

  var mobile =
    '<div class="d-block d-md-none"><div class="row g-2">' +
    totalCard('Total Days', fmt(totals.totalDays)) +
    totalCard('Open Days', fmt(totals.openDays)) +
    totalCard('Approved Days', fmt(totals.approvedDays)) +
    totalCard('Rejected Days', fmt(totals.rejectedDays)) +
    '</div></div>';

  $('#leaveTotals').html('<div class="mb-2">' + desktop + mobile + '</div>');
}

function totalCard(label, value) {
  return (
    '<div class="col-6">' +
    '<div class="card border-0 shadow-sm"><div class="card-body p-2">' +
    '<div class="text-muted small">' + escapeHtml(label) + '</div>' +
    '<div class="fw-bold text-primary">' + escapeHtml(value) + '</div>' +
    '</div></div></div>'
  );
}

function renderLeaves(leaves, isAdmin) {
  if (!leaves || leaves.length === 0) {
    $('#leaveTableBody').html('<tr><td colspan="9" class="text-center py-4 text-muted">No leaves found for selected filters.</td></tr>');
    $('#leaveTableMobile').html(
      '<div class="text-center text-muted py-5">' +
      '<i class="bi bi-inbox fs-1"></i>' +
      '<p class="mt-3 fs-5">No leaves found for selected filters.</p>' +
      '</div>'
    );
    return;
  }

  renderLeavesDesktop(leaves, isAdmin);
  renderLeavesMobile(leaves, isAdmin);
}

function renderLeavesDesktop(leaves, isAdmin) {
  var today = new Date();
  today = new Date(today.getFullYear(), today.getMonth(), today.getDate());

  var html = '';
  leaves.forEach(function (l) {
    var from = parseIsoDate(l.fromDate);
    var to = parseIsoDate(l.toDate);
    var rowClass = 'table-info';
    if (to && to < today) rowClass = 'table-success';
    else if (from && from <= today && to && to >= today) rowClass = 'table-danger';

    var statusBadge = statusBadgeClass(l.status);

    html += '<tr class="' + rowClass + '">';
    html += '<td>' + escapeHtml(l.id) + '</td>';
    html += '<td>' + escapeHtml(l.employeeName) + '</td>';
    html += '<td>' + escapeHtml(l.requestDate) + '</td>';
    html += '<td>' + escapeHtml(l.fromDate) + '</td>';
    html += '<td>' + escapeHtml(l.toDate) + '</td>';
    html += '<td class="text-end">' + escapeHtml(formatDays(l.noOfDays)) + '</td>';
    html += '<td>' + escapeHtml(l.reason) + '</td>';
    html += '<td><span class="badge ' + statusBadge + '">' + escapeHtml(l.status) + '</span></td>';
    html += '<td>' + renderLeaveActions(l, isAdmin, false) + '</td>';
    html += '</tr>';
  });

  $('#leaveTableBody').html(html);
}

function renderLeavesMobile(leaves, isAdmin) {
  var today = new Date();
  today = new Date(today.getFullYear(), today.getMonth(), today.getDate());

  var html = '';
  leaves.forEach(function (l) {
    var from = parseIsoDate(l.fromDate);
    var to = parseIsoDate(l.toDate);
    var rowClass = 'border-info bg-info-subtle';
    if (to && to < today) rowClass = 'border-success bg-success-subtle';
    else if (from && from <= today && to && to >= today) rowClass = 'border-danger bg-danger-subtle';

    var statusBadge = statusBadgeClass(l.status);

    html += '<div class="card shadow-sm mb-3 border ' + rowClass + '">';
    html += '<div class="card-body">';
    html += '<div class="d-flex justify-content-between align-items-start">';
    html += '<div><div class="fw-bold">' + escapeHtml(l.employeeName) + '</div><div class="text-muted small">#' + escapeHtml(l.id) + '</div></div>';
    html += '<span class="badge ' + statusBadge + '">' + escapeHtml(l.status) + '</span>';
    html += '</div>';
    html += '<hr class="my-2" />';
    html += '<div class="row g-2">';
    html += '<div class="col-6"><div class="text-muted small">Requested Date</div><div class="fw-semibold">' + escapeHtml(l.requestDate) + '</div></div>';
    html += '<div class="col-6"><div class="text-muted small">From Date</div><div class="fw-semibold">' + escapeHtml(l.fromDate) + '</div></div>';
    html += '<div class="col-6 text-end"><div class="text-muted small">To Date</div><div class="fw-semibold">' + escapeHtml(l.toDate) + '</div></div>';
    html += '<div class="col-4"><div class="text-muted small">Days</div><div class="fw-semibold">' + escapeHtml(formatDays(l.noOfDays)) + '</div></div>';
    html += '<div class="col-8 text-end"><div class="text-muted small">Reason</div><div class="fw-semibold">' + escapeHtml(l.reason) + '</div></div>';
    html += '</div>';
    html += '<div class="d-flex gap-2 flex-wrap mt-3">' + renderLeaveActions(l, isAdmin, true) + '</div>';
    html += '</div></div>';
  });

  $('#leaveTableMobile').html(html);
}

function renderLeaveActions(l, isAdmin, isMobile) {
  var html = '';
  html += '<span class="leave-action-group d-inline-flex gap-1 flex-wrap">';
  html += '<a class="btn btn-sm btn-info leave-action-btn" href="/Leaves/Details/' + encodeURIComponent(l.id) + '">View</a>';
  if (l.canEdit) {
    html += '<a class="btn btn-sm btn-warning leave-action-btn" href="/Leaves/Edit/' + encodeURIComponent(l.id) + '">Edit</a>';
  }
  if (isAdmin && l.canApproveReject) {
    html += '<button type="button" class="btn btn-sm btn-success leave-action-btn js-leave-approve-btn" data-leave-id="' + escapeAttr(l.id) + '">Approve</button>';
    html += '<button type="button" class="btn btn-sm btn-danger leave-action-btn js-leave-reject-btn" data-leave-id="' + escapeAttr(l.id) + '">Reject</button>';
  }
  html += '</span>';
  return html;
}

function renderLeavePagination(pagination) {
  if (!pagination || pagination.pageCount <= 1) {
    $('#leavePaginationContainer').html('');
    return;
  }

  var html = '<div class="d-flex justify-content-center mt-4"><ul class="pagination">';

  if (pagination.hasPreviousPage) {
    html += '<li class="page-item"><a class="page-link" href="?page=1">First</a></li>';
    html += '<li class="page-item"><a class="page-link" href="?page=' + (pagination.pageNumber - 1) + '">Previous</a></li>';
  }

  var startPage = Math.max(1, pagination.pageNumber - 1);
  var endPage = Math.min(pagination.pageCount, pagination.pageNumber + 1);

  for (var i = startPage; i <= endPage; i++) {
    var activeClass = i === pagination.pageNumber ? 'active' : '';
    html += '<li class="page-item ' + activeClass + '"><a class="page-link" href="?page=' + i + '">' + i + '</a></li>';
  }

  if (pagination.hasNextPage) {
    html += '<li class="page-item"><a class="page-link" href="?page=' + (pagination.pageNumber + 1) + '">Next</a></li>';
    html += '<li class="page-item"><a class="page-link" href="?page=' + pagination.pageCount + '">Last</a></li>';
  }

  html += '</ul></div>';

  var pageSize = pagination.pageSize || 10;
  var start = ((pagination.pageNumber - 1) * pageSize) + 1;
  var end = Math.min(pagination.pageNumber * pageSize, pagination.totalItemCount);

  html += '<div class="text-center mt-2"><small class="text-muted">';
  html += 'Showing ' + start + ' to ' + end + ' of ' + pagination.totalItemCount + ' leaves';
  html += '</small></div>';

  $('#leavePaginationContainer').html(html);
}

function postLeaveDecision(action, id, $btn) {
  var token = $('input[name="__RequestVerificationToken"]').first().val();
  if (!token) {
    showLeaveError('Security token missing. Please refresh the page.');
    return;
  }

  $.ajax({
    url: '/Leaves/' + action,
    type: 'POST',
    headers: { 'X-Requested-With': 'XMLHttpRequest' },
    data: { id: id, __RequestVerificationToken: token },
    success: function (response) {
      if (response && response.success) {
        loadLeaves();
      } else {
        showLeaveError((response && response.message) ? response.message : ('Failed to ' + action.toLowerCase() + ' leave.'));
      }
    },
    error: function () {
      showLeaveError('Error performing action. Please try again.');
    },
    complete: function () {
      if ($btn && $btn.length) $btn.removeClass('active');
    }
  });
}

function showLeaveError(message) {
  var html = '<div class="alert alert-danger">' + escapeHtml(message) + '</div>';
  $('#leaveTableBody').html('<tr><td colspan="9">' + html + '</td></tr>');
  $('#leaveTableMobile').html(html);
  $('#leavePaginationContainer').html('');
}

function statusBadgeClass(status) {
  if (status === 'Approved') return 'bg-success';
  if (status === 'Rejected') return 'bg-danger';
  if (status === 'Cancelled') return 'bg-secondary';
  return 'bg-warning text-dark';
}

function parseIsoDate(value) {
  if (!value) return null;
  var parts = value.split('-');
  if (parts.length !== 3) return null;
  var y = parseInt(parts[0], 10);
  var m = parseInt(parts[1], 10) - 1;
  var d = parseInt(parts[2], 10);
  return new Date(y, m, d);
}

function formatDays(v) {
  var n = Number(v);
  if (Number.isNaN(n)) return '';
  return Number.isInteger(n) ? String(n) : String(n);
}

function escapeAttr(text) {
  return escapeHtml(text).replace(/"/g, '&quot;');
}

function escapeHtml(text) {
  var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
  return (text || '').toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

