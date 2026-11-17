/**
 * Payments Index Page JavaScript
 * AJAX-based loading and pagination (invoice/customer style)
 */

var paymentState = {
  currentPage: 1,
  fromDate: null,
  toDate: null,
  type: null
};

var paymentDateFilter = null;

$(document).ready(function () {
  $('#paymentFilterForm').on('submit', function (e) {
    e.preventDefault();
  });

  paymentState.type = $('#paymentType').val() || null;

  paymentDateFilter = $('#paymentDateFilter').periodDateFilter({
    defaultMode: 'range',
    onChange: function (state) {
      paymentState.fromDate = state.fromDate;
      paymentState.toDate = state.toDate;
      paymentState.currentPage = 1;
      loadPayments();
    }
  }).data('periodDateFilter');

  $('#paymentType').on('change', function () {
    paymentState.type = $('#paymentType').val() || null;
    paymentState.currentPage = 1;
    loadPayments();
  });

  $(document).on('click', '#btnPaymentClear', function (e) {
    e.preventDefault();
    $('#paymentType').val('');
    paymentState.type = null;
    paymentDateFilter.selectMode('range');
  });

  // Pagination (delegated)
  $(document).on('click', '#paymentPaginationContainer .pagination .page-link', function (e) {
    e.preventDefault();
    var href = $(this).attr('href');
    if (href && href !== '#') {
      var pageMatch = href.match(/page=(\d+)/);
      if (pageMatch) {
        paymentState.currentPage = parseInt(pageMatch[1]);
        loadPayments();
      }
    }
  });

});

function loadPayments() {
  $('#paymentLoadingIndicator').show();
  // Hide current content while loading (use inline styles; clear them after load to let CSS media queries apply)
  $('#paymentTableDesktop').hide();
  $('#paymentTableMobile').hide();
  $('#paymentPaginationContainer').hide();

  $.ajax({
    url: '/Payments/GetPayments',
    type: 'GET',
    data: {
      page: paymentState.currentPage,
      fromDate: paymentState.fromDate,
      toDate: paymentState.toDate,
      type: paymentState.type
    },
    success: function (response) {
      if (response && response.success) {
        renderPayments(response.payments || []);
        renderPaymentPagination(response.pagination);
      } else {
        showPaymentError('Failed to load payments.');
      }
    },
    error: function () {
      showPaymentError('Error loading payments. Please try again.');
    },
    complete: function () {
      $('#paymentLoadingIndicator').hide();
      // Clear inline display overrides so responsive CSS works (desktop table vs mobile cards)
      $('#paymentTableDesktop').removeAttr('style');
      $('#paymentTableMobile').removeAttr('style');
      $('#paymentPaginationContainer').removeAttr('style');
    }
  });
}

function renderPayments(payments) {
  if (!payments || payments.length === 0) {
    $('#paymentTableBody').html('<tr><td colspan="6" class="text-center text-muted py-4">No payments found.</td></tr>');
    $('#paymentTableMobile').html(
      '<div class="text-center text-muted py-5">' +
      '<i class="bi bi-inbox fs-1"></i>' +
      '<p class="mt-3 fs-5">No payments found.</p>' +
      '</div>'
    );
    return;
  }

  renderPaymentsDesktop(payments);
  renderPaymentsMobile(payments);
}

function renderPaymentsDesktop(payments) {
  var html = '';
  payments.forEach(function (p) {
    html += '<tr>';
    html += '<td>' + escapeHtml(p.id) + '</td>';
    html += '<td>' + escapeHtml(p.date) + '</td>';
    html += '<td>' + escapeHtml(p.invoiceNo) + '</td>';
    html += '<td>' + escapeHtml(p.typeDisplay) + '</td>';
    html += '<td>' + escapeHtml(p.reference) + '</td>';

    html += '<td class="text-end">';
    html += '<div class="fw-bold">LKR ' + escapeHtml(formatAmount(p.amount)) + '</div>';
    // Match existing behavior: only show currency conversion when SelectedCurrency + curryAmount > 0
    if (p.curryAmount && Number(p.curryAmount) > 0 && p.paidCurrency === 'SelectedCurrency' && p.currency) {
      html += '<div class="text-primary" style="font-size: 0.75rem;">' + escapeHtml(p.currency) + ' ' + escapeHtml(formatAmount(p.curryAmount)) + '</div>';
    }
    html += '</td>';

    html += '</tr>';
  });

  $('#paymentTableBody').html(html);
}

function renderPaymentsMobile(payments) {
  // Group by date string (already formatted) preserving order
  var groups = {};
  var order = [];
  payments.forEach(function (p) {
    var key = p.date || '';
    if (!groups[key]) {
      groups[key] = [];
      order.push(key);
    }
    groups[key].push(p);
  });

  var html = '';
  order.forEach(function (dateKey) {
    html += '<div class="payment-date-group">';
    html += '<div class="payment-date-header"><h6 class="payment-date-title">' + escapeHtml(dateKey) + '</h6></div>';

    (groups[dateKey] || []).forEach(function (p) {
      var paymentTypeName = 'Invoice #' + p.invoiceNo;

      html += '<div class="payment-row-wrapper">';
      html += '<div class="payment-row-compact payment-status-success">';
      html += '<div class="payment-row-header">';
      html += '<div class="payment-type-icon"><i class="bi bi-credit-card"></i></div>';
      html += '<div class="payment-invoice-name">' + escapeHtml(paymentTypeName) + '</div>';
      html += '<div class="payment-amount">';
      html += '<div class="fw-bold">' + escapeHtml(formatAmount(p.amount)) + '</div>';
      if (p.curryAmount && Number(p.curryAmount) > 0 && p.currency) {
        html += '<div class="text-primary" style="font-size: 0.75rem;">' + escapeHtml(p.currency) + ' ' + escapeHtml(formatAmount(p.curryAmount)) + '</div>';
      }
      html += '</div></div>'; // header

      html += '<div class="payment-row-footer">';
      html += '<span class="payment-number"><span class="payment-number-label">PAY No</span> <span class="payment-number-value">#' + escapeHtml(p.id) + '</span></span>';
      html += '<span class="payment-type-badge">' + escapeHtml(p.typeDisplay) + '</span>';
      html += '<a href="/Payments/Details/' + encodeURIComponent(p.id) + '" class="btn-view-payment"><i class="bi bi-eye"></i> View</a>';
      if (p.createdByName) {
        html += '<span class="payment-creator">' + escapeHtml(p.createdByName) + '</span>';
      }
      html += '</div>'; // footer

      html += '</div></div>'; // row
    });

    html += '</div>';
  });

  $('#paymentTableMobile').html(html);
}

function renderPaymentPagination(pagination) {
  if (!pagination || pagination.pageCount <= 1) {
    $('#paymentPaginationContainer').html('');
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

  html += '<div class="text-center mt-1"><small class="text-muted">';
  html += 'Showing ' + start + ' to ' + end + ' of ' + pagination.totalItemCount + ' payments';
  html += '</small></div>';

  $('#paymentPaginationContainer').html(html);
}

function showPaymentError(message) {
  var html = '<div class="alert alert-danger">' + escapeHtml(message) + '</div>';
  $('#paymentTableBody').html('<tr><td colspan="6">' + html + '</td></tr>');
  $('#paymentTableMobile').html(html);
  $('#paymentPaginationContainer').html('');
}

function formatAmount(v) {
  var n = Number(v || 0);
  return n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function escapeHtml(text) {
  var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
  return (text || '').toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

