/**
 * Invoice Index Page JavaScript
 * Handles AJAX-based filtering, pagination, and delete functionality
 */

// Global state
var invoiceState = {
    currentPage: 1,
    invoiceStatus: null,
    customerId: 0,
    invoiceType: null,
    fromDate: null,
    toDate: null,
    isAdmin: false,
    isInternalCallScope: false
};

// Safely read the admin flag from the hidden field and normalize to a boolean
function getIsAdminFlag() {
    var val = $('#isAdminFlag').data('is-admin');

    // If it's a string like "true"/"false"
    if (typeof val === 'string') {
        return val.toLowerCase() === 'true';
    }

    // For booleans or anything else, coerce to boolean
    return !!val;
}

$(document).ready(function () {
    // Initialize admin flag (normalized to a real boolean)
    invoiceState.isAdmin = getIsAdminFlag();   

    // Filter form submission
    $('#invoiceFilterForm').on('submit', function (e) {
        e.preventDefault();
        invoiceState.currentPage = 1;
        loadInvoices();
    });

    $('#customerId, #invoiceType').on('change', function () {
        invoiceState.customerId = parseInt($('#customerId').val()) || 0;
        invoiceState.invoiceType = $('#invoiceType').val() || null;
        invoiceState.currentPage = 1;
        loadInvoices();
    });

    $('#fromDate, #toDate').on('change', function () {
        if (!invoiceState.isInternalCallScope) {
            invoiceState.fromDate = $('#fromDate').val() || null;
            invoiceState.toDate = $('#toDate').val() || null;
            // Remove active state from all date buttons when dates are manually changed
            $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
            invoiceState.currentPage = 1;
            loadInvoices();
        }
    });

    var formatDateLocal = function (date) {
        var year = date.getFullYear();
        var month = String(date.getMonth() + 1).padStart(2, '0');
        var day = String(date.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    // Date quick buttons
    $('#btnToday').on('click', function () {
        var today = new Date();
        var todayStr = formatDateLocal(today);
        onDateBtnAction(todayStr, todayStr, this);        
    });

    $('#btnYesterday').on('click', function () {
        var yesterday = new Date();
        yesterday.setDate(yesterday.getDate() - 1);
        var dateStr = formatDateLocal(yesterday);
        onDateBtnAction(dateStr, dateStr, this);        
    });

    $('#btnMonth').on('click', function () {
        var now = new Date();
        // Get first day of current month (1st)
        var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
        // Get last day of current month (end of current month)
        var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);

        var firstDayStr = formatDateLocal(firstDay);
        var lastDayStr = formatDateLocal(lastDay);

        onDateBtnAction(firstDayStr, lastDayStr, this);
    });

    $('#btnYear').on('click', function () {
        var now = new Date();
        // Get first day of current year (January 1st)
        var firstDay = new Date(now.getFullYear(), 0, 1);
        // Get last day of current year (December 31st)
        var lastDay = new Date(now.getFullYear(), 11, 31);

        var firstDayStr = formatDateLocal(firstDay);
        var lastDayStr = formatDateLocal(lastDay);

        onDateBtnAction(firstDayStr, lastDayStr, this);        
    });

    // Status tab clicks
    $('.invoice-status-tab').on('click', function (e) {
        e.preventDefault();
        $('.invoice-status-tab').removeClass('active');
        $(this).addClass('active');

        var status = $(this).data('status');
        invoiceState.invoiceStatus = status || null;
        invoiceState.currentPage = 1;
        loadInvoices();
    });

    // Pagination clicks (delegated)
    $(document).on('click', '.pagination .page-link', function (e) {
        e.preventDefault();
        var href = $(this).attr('href');
        if (href && href !== '#') {
            var pageMatch = href.match(/page=(\d+)/);
            if (pageMatch) {
                invoiceState.currentPage = parseInt(pageMatch[1]);
                loadInvoices();
            }
        }
    });

    // Delete invoice handler
    $(document).on('click', '.btn-delete-invoice', function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        var invoiceId = $(this).data('invoice-id');
        var invoiceNo = $(this).data('invoice-no');

        showConfirmDialog(
            'Are you sure you want to delete Invoice #' + invoiceNo + '?',
            function (confirmed) {
                if (confirmed) {
                    deleteInvoice(invoiceId);
                }
            },
            {
                type: 'Danger',
                title: 'Delete Invoice',
                yesButtonText: 'Yes, Delete',
                noButtonText: 'No',
                warningText: 'This action cannot be undone.',
                dialogId: 'deleteConfirmDialog'
            }
        );

        return false;
    });

    // View payments handler
    $(document).on('click', '.btn-view-payments, .btn-view-payments-icon', function (e) {
        e.preventDefault();
        e.stopPropagation();

        if ($(this).hasClass('disabled') || $(this).prop('disabled')) {
            return;
        }

        var invoiceNo = $(this).data('invoice-no');
        if (!invoiceNo) {
            console.error('Invoice number not found');
            return;
        }

        $('#modalInvoiceNo').text(invoiceNo);

        $('#invoicePaymentsModalBody').html(
            '<div class="text-center py-5">' +
            '<div class="spinner-border text-primary" role="status">' +
            '<span class="visually-hidden">Loading...</span>' +
            '</div>' +
            '<p class="mt-3">Loading payments...</p>' +
            '</div>'
        );

        var modalElement = document.getElementById('invoicePaymentsModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            var modal = new bootstrap.Modal(modalElement);
            modal.show();
        } else {
            $('#invoicePaymentsModal').modal('show');
        }

        $.getJSON('/Internal/Invoices/GetPayments/' + invoiceNo, function (response) {
            if (response && response.success && response.data) {
                renderPayments(response.data);
            } else {
                $('#invoicePaymentsModalBody').html(
                    '<div class="alert alert-info">No payments found for this invoice.</div>'
                );
            }
        }).fail(function (xhr, status, error) {
            console.error('Error loading payments:', error);
            $('#invoicePaymentsModalBody').html(
                '<div class="alert alert-danger">Error loading payments. Please try again.</div>'
            );
        });
    });


    // Load initial data(current month)
    $('#btnMonth').click();    
});

function onDateBtnAction(fromDate, toDate, ele) {
    invoiceState.isInternalCallScope = true;
    $('#fromDate').val(fromDate);
    $('#toDate').val(toDate);
    invoiceState.isInternalCallScope = false;

    invoiceState.fromDate = fromDate;
    invoiceState.toDate = toDate;

    $('.btn', $("#invoiceFilterForm")).removeClass("active")
    $(ele).addClass("active");
    loadInvoices();
}

/**
 * Load invoices via AJAX
 */
function loadInvoices() {
    // Update state from form
    invoiceState.customerId = parseInt($('#customerId').val()) || 0;
    invoiceState.invoiceType = $('#invoiceType').val() || null;

    // Show loading indicator
    $('#invoiceLoadingIndicator').show();
    $('#invoiceTableDesktop').hide();
    $('#invoiceTableMobile').hide();
    $('#invoicePaginationContainer').hide();

    // Build request data
    var requestData = {
        page: invoiceState.currentPage,
        customerId: invoiceState.customerId,
        invoiceType: invoiceState.invoiceType,
        fromDate: invoiceState.fromDate,
        toDate: invoiceState.toDate
    };

    if (invoiceState.invoiceStatus) {
        requestData.invoiceStatus = invoiceState.invoiceStatus;
    }

    $.ajax({
        url: '/Internal/Invoices/GetInvoices',
        type: 'GET',
        data: requestData,
        success: function (response) {
            if (response.success) {
                renderInvoices(response.invoices);
                renderPagination(response.pagination);
                updateBadgeCounts(response.counts);                
            } else {
                showError('Failed to load invoices.');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading invoices:', error);
            showError('Error loading invoices. Please try again.');
        },
        complete: function () {
            $('#invoiceLoadingIndicator').hide();
        }
    });
}

/**
 * Render invoices (desktop and mobile)
 */
function renderInvoices(invoices) {
    if (!invoices || invoices.length === 0) {
        renderEmptyState();
        return;
    }

    // Render desktop table
    renderDesktopTable(invoices);

    // Render mobile cards
    renderMobileCards(invoices);

    // Remove inline display styles to let CSS media queries handle visibility
    $('#invoiceTableDesktop').css('display', '');
    $('#invoiceTableMobile').css('display', '');
}

/**
 * Render desktop table
 */
function renderDesktopTable(invoices) {
    var html = '';

    invoices.forEach(function (invoice) {
        var isPaymentsDisabled = invoice.status === 'InProgress' || invoice.status === 'Complete';
        var settledOnDisplay = '';

        // Settled on: show only for Paid invoices, using short date
        if (invoice.status === 'Paid' && invoice.settledOn) {
            settledOnDisplay = formatDate(invoice.settledOn);
        }

        html += '<tr>';
        html += '<td>';
        html += '<div>#' + escapeHtml(invoice.invoiceNo) + '</div>';
        if (invoice.createdByMember && invoice.createdByMember.name) {
            html += '<div class="text-primary" style="font-size: 0.8rem;">' + escapeHtml(invoice.createdByMember.name) + '</div>';
        }
        html += '</td>';
        html += '<td>' + escapeHtml(invoice.customerName) + '</td>';
        html += '<td>' + escapeHtml(invoice.typeDisplay) + '</td>';
        html += '<td>' + formatDate(invoice.date) + '</td>';
        html += '<td>' + escapeHtml(settledOnDisplay) + '</td>';
        html += '<td>' + escapeHtml(invoice.statusDisplay) + '</td>';
        html += '<td class="text-end">';
        html += '<div>' + formatCurrency(invoice.grossAmount) + '</div>';
        // Show curry amount only for Stay and Tour invoices
        if ((invoice.type === 'Stay' || invoice.type === 'Tour') && 
            invoice.curryGrossAmount != null && invoice.curryGrossAmount !== undefined && invoice.currency) {
            html += '<div class="text-primary" style="font-size: 0.75rem;">' +
                escapeHtml(invoice.currency) + ' ' +
                Number(invoice.curryGrossAmount).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                '</div>';
        }
        html += '</td>';
        html += '<td class="text-end">';
        html += '<div>' + formatCurrency(invoice.totalPaid || 0) + '</div>';
        // Show curry amount only for Stay and Tour invoices
        if ((invoice.type === 'Stay' || invoice.type === 'Tour') && 
            invoice.curryTotalPaid != null && invoice.curryTotalPaid !== undefined && invoice.currency) {
            html += '<div class="text-primary" style="font-size: 0.75rem;">' +
                escapeHtml(invoice.currency) + ' ' +
                Number(invoice.curryTotalPaid).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                '</div>';
        }
        html += '</td>';
        html += '<td class="text-end">';
        html += '<div>' + formatCurrency(invoice.balance || 0) + '</div>';
        // Show curry amount only for Stay and Tour invoices
        if ((invoice.type === 'Stay' || invoice.type === 'Tour') && 
            invoice.curryBalance != null && invoice.curryBalance !== undefined && invoice.currency) {
            html += '<div class="text-primary" style="font-size: 0.75rem;">' +
                escapeHtml(invoice.currency) + ' ' +
                Number(invoice.curryBalance).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                '</div>';
        }
        html += '</td>';
        html += '<td>';
        html += '<div class="d-flex gap-1">';
        html += '<a href="/Internal/Invoices/Edit/' + invoice.invoiceNo + '" class="btn btn-sm btn-warning"><i class="bi bi-eye"></i>View</a>';
        html += '<button type="button" class="btn btn-sm btn-info btn-view-payments ' + (isPaymentsDisabled ? 'disabled' : '') + '" ';
        html += 'data-invoice-no="' + invoice.invoiceNo + '" ' + (isPaymentsDisabled ? 'disabled' : '') + ' ';
        html += 'title="' + (isPaymentsDisabled ? 'Payments not available for this invoice status' : 'View Payments') + '">';
        html += '<div class="d-flex flex-column align-items-center"><i class="bi bi-credit-card"></i><span class="btn-text">Payments</span></div>';
        html += '</button>';
        if (invoiceState.isAdmin === true) {
            html += '<button type="button" class="btn btn-sm btn-danger btn-delete-invoice" ';
            html += 'data-invoice-id="' + invoice.invoiceNo + '" data-invoice-no="' + invoice.invoiceNo + '">';
            html += '<i class="bi bi-trash"></i>Delete</button>';
        }
        html += '</div>';
        html += '</td>';
        html += '</tr>';
    });

    $('#invoiceTableBody').html(html);
}

/**
 * Render mobile cards
 */
function renderMobileCards(invoices) {
    // Group invoices by date
    var grouped = {};
    invoices.forEach(function (invoice) {
        var dateKey = invoice.date;
        if (!grouped[dateKey]) {
            grouped[dateKey] = [];
        }
        grouped[dateKey].push(invoice);
    });

    // Sort dates descending
    var sortedDates = Object.keys(grouped).sort(function (a, b) {
        return new Date(b) - new Date(a);
    });

    var html = '';

    sortedDates.forEach(function (date) {
        var dateInvoices = grouped[date];
        var dateDisplay = formatDateDisplay(date);

        html += '<div class="invoice-date-group">';
        html += '<div class="invoice-date-header">';
        html += '<h6 class="invoice-date-title">' + dateDisplay + '</h6>';
        html += '</div>';

        dateInvoices.forEach(function (invoice) {
            var statusClass = getStatusClass(invoice.status);
            var typeIcon = getTypeIcon(invoice.type);
            var isPaymentsDisabled = invoice.status === 'InProgress' || invoice.status === 'Complete';
            var settledOnDisplay = '';

            // Settled on: show only for Paid invoices, using short date
            if (invoice.status === 'Paid' && invoice.settledOn) {
                settledOnDisplay = formatDate(invoice.settledOn);
            }

            html += '<div class="invoice-row-wrapper">';
            html += '<div class="invoice-row-compact ' + statusClass + '">';
            html += '<div class="invoice-row-header">';
            html += '<div class="invoice-type-icon"><i class="bi ' + typeIcon + '"></i></div>';
            html += '<div class="invoice-customer-name">' + escapeHtml(invoice.customerName) + '</div>';
            html += '<div class="invoice-amount">';
            html += '<div>' + formatCurrency(invoice.grossAmount) + '</div>';
            html += '</div>';
            html += '</div>';
            html += '<div class="invoice-row-footer">';

            html += '<span class="invoice-number">';
            html += '<span class="invoice-number-label">INV No</span>';
            html += '<span class="invoice-number-value">#' + invoice.invoiceNo + '</span>';
            if (invoiceState.isAdmin === true) {
                html += '<button type="button" class="btn-delete-invoice-icon btn-delete-invoice" ';
                html += 'data-invoice-id="' + invoice.invoiceNo + '" data-invoice-no="' + invoice.invoiceNo + '" ';
                html += 'title="Delete Invoice"><i class="bi bi-trash"></i></button>';
            }
            html += '</span>';
            html += '<button type="button" class="btn-view-payments-icon btn-view-payments-mobile ' + (isPaymentsDisabled ? 'disabled' : '') + '" ';
            html += 'data-invoice-no="' + invoice.invoiceNo + '" ' + (isPaymentsDisabled ? 'disabled' : '') + ' ';
            html += 'title="' + (isPaymentsDisabled ? 'Payments not available for this invoice status' : 'View Payments') + '">';
            html += '<i class="bi bi-credit-card"></i></button>';
            html += '<a href="/Internal/Invoices/Edit/' + invoice.invoiceNo + '" class="btn-view-invoice">';
            html += '<i class="bi bi-eye"></i> View</a>';
            html += '</div>';
            
            // Third row: Settled On (left) and invoice-creator (right)
            if ((invoice.createdByMember && invoice.createdByMember.name) || settledOnDisplay) {
                html += '<div class="invoice-row-meta">';
                // Mobile: show Settled On on the left (for Paid invoices only)
                // Use same label/value classes as INV No for consistent font and color
                if (settledOnDisplay) {
                    html += '<span class="invoice-number invoice-settled-on">';
                    html += '<span class="invoice-number-label">Settled On</span>';
                    html += '<span class="invoice-number-value">' + settledOnDisplay + '</span>';
                    html += '</span>';
                }
                // Invoice creator on the right
                if (invoice.createdByMember && invoice.createdByMember.name) {
                    html += '<span class="invoice-creator">' + escapeHtml(invoice.createdByMember.name) + '</span>';
                }
                html += '</div>';
            }
            html += '</div>';
            html += '</div>';
            html += '</div>';
        });

        html += '</div>';
    });

    $('#invoiceTableMobile').html(html);
}

/**
 * Render pagination
 */
function renderPagination(pagination) {
    if (!pagination || pagination.pageCount <= 1) {
        $('#invoicePaginationContainer').html('');
        return;
    }

    var html = '<div class="d-flex justify-content-center mt-4"><ul class="pagination">';

    // First page
    if (pagination.hasPreviousPage) {
        html += '<li class="page-item"><a class="page-link" href="?page=1">First</a></li>';
        html += '<li class="page-item"><a class="page-link" href="?page=' + (pagination.pageNumber - 1) + '">Previous</a></li>';
    }

    // Page numbers
    var startPage = Math.max(1, pagination.pageNumber - 1);
    var endPage = Math.min(pagination.pageCount, pagination.pageNumber + 1);

    for (var i = startPage; i <= endPage; i++) {
        var activeClass = i === pagination.pageNumber ? 'active' : '';
        html += '<li class="page-item ' + activeClass + '"><a class="page-link" href="?page=' + i + '">' + i + '</a></li>';
    }

    // Last page
    if (pagination.hasNextPage) {
        html += '<li class="page-item"><a class="page-link" href="?page=' + (pagination.pageNumber + 1) + '">Next</a></li>';
        html += '<li class="page-item"><a class="page-link" href="?page=' + pagination.pageCount + '">Last</a></li>';
    }

    html += '</ul></div>';
    html += '<div class="text-center mt-2"><small class="text-muted">';
    html += 'Showing ' + ((pagination.pageNumber - 1) * 20 + 1) + ' to ';
    html += Math.min(pagination.pageNumber * 20, pagination.totalItemCount) + ' of ';
    html += pagination.totalItemCount + ' invoices';
    html += '</small></div>';

    $('#invoicePaginationContainer').html(html).show();
}

/**
 * Update badge counts
 */
function updateBadgeCounts(counts) {
    $('#badge-all').text(counts.all || 0);
    $('#badge-open').text(counts.open || 0);
    $('#badge-complete').text(counts.complete || 0);
    $('#badge-partial').text(counts.partial || 0);
    $('#badge-paid').text(counts.paid || 0);
}

/**
 * Render empty state
 */
function renderEmptyState() {
    var emptyHtml = '<div class="text-center text-muted py-5">';
    emptyHtml += '<i class="bi bi-inbox fs-1"></i>';
    emptyHtml += '<p class="mt-3 fs-5">No invoices found.</p>';
    emptyHtml += '</div>';

    $('#invoiceTableBody').html('<tr><td colspan="9" class="text-center text-muted">No invoices found.</td></tr>');
    $('#invoiceTableMobile').html(emptyHtml);
    // Remove inline display styles to let CSS media queries handle visibility
    $('#invoiceTableDesktop').css('display', '');
    $('#invoiceTableMobile').css('display', '');
}

/**
 * Delete invoice via AJAX
 */
function deleteInvoice(invoiceId) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    var deleteUrl = '/Internal/Invoices/Delete/' + invoiceId;

    $.ajax({
        url: deleteUrl,
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                if (typeof showToastSuccess === 'function') {
                    showToastSuccess(response.message || 'Invoice deleted successfully.');
                } else {
                    alert(response.message || 'Invoice deleted successfully.');
                }
                loadInvoices(); // Reload invoices
            } else {
                if (typeof showToastError === 'function') {
                    showToastError(response.message || 'Error deleting invoice.');
                } else {
                    alert(response.message || 'Error deleting invoice.');
                }
            }
        },
        error: function (xhr, status, error) {
            var errorMessage = 'Error deleting invoice. Please try again.';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            }

            if (typeof showToastError === 'function') {
                showToastError(errorMessage);
            } else {
                alert(errorMessage);
            }
        }
    });
}

/**
 * Render payments in the modal
 */
function renderPayments(payments) {
    if (!payments || payments.length === 0) {
        $('#invoicePaymentsModalBody').html(
            '<div class="alert alert-info">No payments found for this invoice.</div>'
        );
        return;
    }

    var totalAmount = payments.reduce(function (sum, payment) {
        return sum + parseFloat(payment.amount || 0);
    }, 0);

    var groupedPayments = {};
    payments.forEach(function (payment) {
        var dateKey = payment.date;
        if (!groupedPayments[dateKey]) {
            groupedPayments[dateKey] = [];
        }
        groupedPayments[dateKey].push(payment);
    });

    var sortedDates = Object.keys(groupedPayments).sort(function (a, b) {
        return new Date(b) - new Date(a);
    });

    var html = '<div class="payments-container">';

    sortedDates.forEach(function (date) {
        var datePayments = groupedPayments[date];
        var dateTotal = datePayments.reduce(function (sum, p) {
            return sum + parseFloat(p.amount || 0);
        }, 0);

        html += '<div class="payment-date-group mb-3">';
        html += '<div class="payment-date-header bg-light p-2 rounded">';
        html += '<h6 class="mb-0 fw-bold">' + formatDate(date) + '</h6>';
        html += '<small class="text-muted">Total: ' + formatCurrency(dateTotal) + '</small>';
        html += '</div>';

        html += '<div class="payment-list">';
        datePayments.forEach(function (payment) {
            html += '<div class="payment-row card mb-2">';
            html += '<div class="card-body p-3">';
            html += '<div class="d-flex justify-content-between align-items-center payment-row-content">';
            // Left: Payment ID
            html += '<div class="payment-id">ID: ' + payment.id + '</div>';
            // Middle: Payment Type
            html += '<div class="payment-type">' + escapeHtml(payment.type) + '</div>';
            // Right: Amount
            html += '<div class="payment-amount fw-bold">' + formatCurrency(payment.amount) + '</div>';
            html += '</div>';
            // Reference below if exists
            if (payment.reference) {
                html += '<div class="mt-2"><small class="text-muted">Ref: ' + escapeHtml(payment.reference) + '</small></div>';
            }
            html += '</div>';
            html += '</div>';
        });
        html += '</div>';
        html += '</div>';
    });

    html += '<div class="payment-total mt-4 p-3 bg-primary text-white rounded">';
    html += '<div class="d-flex justify-content-between align-items-center">';
    html += '<span class="fw-bold fs-5">Total Payments:</span>';
    html += '<span class="fw-bold fs-5">' + formatCurrency(totalAmount) + '</span>';
    html += '</div>';
    html += '</div>';

    html += '</div>';

    $('#invoicePaymentsModalBody').html(html);
}

/**
 * Helper functions
 */
function formatDate(dateString) {
    var date = new Date(dateString);
    return date.toLocaleDateString();
}

function formatDateDisplay(dateString) {
    var date = new Date(dateString);
    var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[date.getMonth()] + ' ' + date.getDate() + ', ' + date.getFullYear();
}

function formatCurrency(value) {
    var num = Number(value || 0);
    return 'LKR ' + num.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function getStatusClass(status) {
    switch (status) {
        case 'InProgress': return 'status-in-progress';
        case 'Complete': return 'status-complete';
        case 'PartiallyPaid': return 'status-partially-paid';
        case 'Paid': return 'status-paid';
        default: return 'bg-secondary';
    }
}

function getTypeIcon(type) {
    switch (type) {
        case 'Dining': return 'bi-cup-hot-fill';
        case 'TakeAway': return 'bi-bag-check-fill';
        case 'Stay': return 'bi-building-check';
        case 'Tour': return 'bi-bus-front-fill';
        case 'Laundry': return 'bi-basket2-fill';
        case 'Other': return 'bi-gear-fill';
        default: return 'bi-receipt';
    }
}

function getPaymentTypeBadge(type) {
    var badgeClass = 'bg-secondary';
    var icon = 'bi-credit-card';

    switch (type.toLowerCase()) {
        case 'cash':
            badgeClass = 'bg-success';
            icon = 'bi-cash-stack';
            break;
        case 'card':
            badgeClass = 'bg-primary';
            icon = 'bi-credit-card';
            break;
        case 'banktransfer':
        case 'bank_transfer':
            badgeClass = 'bg-info';
            icon = 'bi-bank';
            break;
        case 'cheque':
            badgeClass = 'bg-warning';
            icon = 'bi-receipt';
            break;
    }

    return '<span class="badge ' + badgeClass + '"><i class="bi ' + icon + '"></i> ' + escapeHtml(type) + '</span>';
}

function escapeHtml(text) {
    var map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return (text || '').toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

function showError(message) {
    var errorHtml = '<div class="alert alert-danger">' + escapeHtml(message) + '</div>';
    $('#invoiceTableBody').html('<tr><td colspan="7">' + errorHtml + '</td></tr>');
    $('#invoiceTableMobile').html('<div class="alert alert-danger">' + escapeHtml(message) + '</div>');
    // Remove inline display styles to let CSS media queries handle visibility
    $('#invoiceTableDesktop').css('display', '');
    $('#invoiceTableMobile').css('display', '');
}
