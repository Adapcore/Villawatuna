/**
 * Currency Exchange Index — list, filters, pagination
 */
var exchangeState = {
    currentPage: 1,
    startDate: null,
    endDate: null,
    customerId: 0,
    foreignCurrencyId: 0,
    isInternalCallScope: false,
    isAdmin: false
};

function getIsAdminFlag() {
    var val = $('#isAdminFlag').data('is-admin');
    if (typeof val === 'string') {
        return val.toLowerCase() === 'true';
    }
    return !!val;
}

function formatDateLocal(date) {
    var year = date.getFullYear();
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var day = String(date.getDate()).padStart(2, '0');
    return year + '-' + month + '-' + day;
}

function formatDateDisplay(dateStr) {
    if (!dateStr) return '';
    var d = new Date(dateStr + 'T00:00:00');
    if (isNaN(d.getTime())) return dateStr;
    return d.toLocaleDateString(undefined, { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' });
}

function formatMoney(value, prefix) {
    var n = parseFloat(value);
    if (isNaN(n)) n = 0;
    var formatted = n.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    return prefix ? prefix + ' ' + formatted : formatted;
}

function formatRate(value) {
    var n = parseFloat(value);
    if (isNaN(n)) n = 0;
    return n.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 4
    });
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

$(document).ready(function () {
    exchangeState.isAdmin = getIsAdminFlag();

    $('#exchangeFilterForm').on('submit', function (e) {
        e.preventDefault();
        exchangeState.currentPage = 1;
        loadExchanges();
    });

    $(document).on('click', '.pagination .page-link', function (e) {
        e.preventDefault();
        var href = $(this).attr('href');
        if (href && href !== '#') {
            var pageMatch = href.match(/page=(\d+)/);
            if (pageMatch) {
                exchangeState.currentPage = parseInt(pageMatch[1]);
                loadExchanges();
            }
        }
    });

    $('#customerId, #foreignCurrencyId').on('change', function () {
        exchangeState.customerId = parseInt($('#customerId').val()) || 0;
        exchangeState.foreignCurrencyId = parseInt($('#foreignCurrencyId').val()) || 0;
        exchangeState.currentPage = 1;
        loadExchanges();
    });

    $('#startDate, #endDate').on('change', function () {
        if (!exchangeState.isInternalCallScope) {
            exchangeState.startDate = $('#startDate').val() || null;
            exchangeState.endDate = $('#endDate').val() || null;
            $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
            exchangeState.currentPage = 1;
            loadExchanges();
        }
    });

    $('#btnToday').on('click', function () {
        var today = formatDateLocal(new Date());
        onDateBtnAction(today, today, this);
    });

    $('#btnYesterday').on('click', function () {
        var d = new Date();
        d.setDate(d.getDate() - 1);
        var str = formatDateLocal(d);
        onDateBtnAction(str, str, this);
    });

    $('#btnMonth').on('click', function () {
        var now = new Date();
        var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
        var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
        onDateBtnAction(formatDateLocal(firstDay), formatDateLocal(lastDay), this);
    });

    $('#btnYear').on('click', function () {
        var now = new Date();
        var firstDay = new Date(now.getFullYear(), 0, 1);
        var lastDay = new Date(now.getFullYear(), 11, 31);
        onDateBtnAction(formatDateLocal(firstDay), formatDateLocal(lastDay), this);
    });

    $('#btnClear').on('click', function (e) {
        e.preventDefault();
        exchangeState.isInternalCallScope = true;
        $('#startDate').val('');
        $('#endDate').val('');
        $('#customerId').val('0');
        $('#foreignCurrencyId').val('0');
        exchangeState.isInternalCallScope = false;

        exchangeState.startDate = null;
        exchangeState.endDate = null;
        exchangeState.customerId = 0;
        exchangeState.foreignCurrencyId = 0;
        exchangeState.currentPage = 1;
        $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
        loadExchanges();
    });

    $(document).on('click', '.btn-delete-exchange', function (e) {
        e.preventDefault();
        var id = $(this).data('id');

        showConfirmDialog(
            'Are you sure you want to delete Exchange #' + id + '?',
            function (confirmed) {
                if (confirmed) {
                    deleteExchange(id);
                }
            },
            {
                type: 'Danger',
                title: 'Delete Exchange',
                yesButtonText: 'Yes, Delete',
                noButtonText: 'No',
                warningText: 'This action cannot be undone.',
                dialogId: 'deleteExchangeConfirmDialog'
            }
        );
    });

    $('#btnMonth').click();
});

function onDateBtnAction(fromDate, toDate, ele) {
    exchangeState.isInternalCallScope = true;
    $('#startDate').val(fromDate);
    $('#endDate').val(toDate);
    exchangeState.isInternalCallScope = false;

    exchangeState.startDate = fromDate;
    exchangeState.endDate = toDate;
    exchangeState.currentPage = 1;

    $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
    $(ele).addClass('active');
    loadExchanges();
}

function loadExchanges() {
    exchangeState.customerId = parseInt($('#customerId').val()) || 0;
    exchangeState.foreignCurrencyId = parseInt($('#foreignCurrencyId').val()) || 0;

    $('#exchangeLoadingIndicator').show();
    $('#exchangeTableDesktop').hide();
    $('#exchangeTableMobile').hide();
    $('#exchangePaginationContainer').hide();

    $.ajax({
        url: '/CurrencyExchange/GetExchanges',
        type: 'GET',
        data: {
            page: exchangeState.currentPage,
            startDate: exchangeState.startDate,
            endDate: exchangeState.endDate,
            customerId: exchangeState.customerId,
            foreignCurrencyId: exchangeState.foreignCurrencyId
        },
        success: function (response) {
            $('#exchangeLoadingIndicator').hide();
            if (!response.success) {
                renderError('Failed to load exchanges.');
                return;
            }
            renderExchanges(response.exchanges || []);
            renderPagination(response.pagination);
            renderTotalLkr(response.totalLkrAmount);
            $('#exchangeTableDesktop').css('display', '');
            $('#exchangeTableMobile').css('display', '');
            $('#exchangePaginationContainer').show();
        },
        error: function () {
            $('#exchangeLoadingIndicator').hide();
            renderError('Error loading exchanges. Please try again.');
        }
    });
}

function renderExchanges(exchanges) {
    renderDesktopTable(exchanges);
    renderMobileCards(exchanges);
}

function renderDesktopTable(exchanges) {
    var $tbody = $('#exchangeTableBody');
    $tbody.empty();

    if (!exchanges.length) {
        $tbody.append('<tr><td colspan="9" class="text-center text-muted py-4">No exchanges found.</td></tr>');
        return;
    }

    exchanges.forEach(function (e) {
        var row = '<tr>' +
            '<td>#' + escapeHtml(e.id) + '</td>' +
            '<td>' + formatDateDisplay(e.date) + '</td>' +
            '<td>' + escapeHtml(e.customerName) + '</td>' +
            '<td>' + escapeHtml(e.foreignCurrencyCode) + '</td>' +
            '<td class="text-end">' + formatRate(e.exchangeRate) + '</td>' +
            '<td class="text-end">' + formatMoney(e.foreignAmount, e.foreignCurrencyCode) + '</td>' +
            '<td class="text-end fw-bold">' + formatMoney(e.lkrAmount, 'LKR') + '</td>' +
            '<td>' + escapeHtml(e.remarks) + '</td>' +
            '<td class="text-center">' +
            '<div class="d-flex gap-1 justify-content-center">' +
            '<a href="/CurrencyExchange/Edit/' + e.id + '" class="btn btn-sm btn-outline-primary" title="Edit"><i class="bi bi-pencil"></i></a>' +
            (exchangeState.isAdmin
                ? '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-exchange" data-id="' + e.id + '" title="Delete"><i class="bi bi-trash"></i></button>'
                : '') +
            '</div>' +
            '</td>' +
            '</tr>';
        $tbody.append(row);
    });
}

function renderMobileCards(exchanges) {
    var $mobile = $('#exchangeTableMobile');
    $mobile.empty();

    if (!exchanges.length) {
        $mobile.html('<div class="cex-empty">No exchanges found.</div>');
        return;
    }

    var html = '';
    exchanges.forEach(function (e) {
        html += '<div class="cex-card">';
        html += '<div class="cex-card-header">';
        html += '<div>';
        html += '<span class="cex-card-txn">#' + escapeHtml(e.id) + '</span>';
        html += '<h6 class="cex-card-date">' + formatDateDisplay(e.date) + '</h6>';
        html += '</div>';
        html += '<span class="cex-card-lkr">' + formatMoney(e.lkrAmount, 'LKR') + '</span>';
        html += '</div>';
        html += '<div class="cex-card-body">';
        html += '<div class="cex-metric"><span class="cex-metric-label">Customer</span><span class="cex-metric-value">' + escapeHtml(e.customerName) + '</span></div>';
        html += '<div class="cex-metric"><span class="cex-metric-label">Currency</span><span class="cex-metric-value">' + escapeHtml(e.foreignCurrencyCode) + '</span></div>';
        html += '<div class="cex-metric"><span class="cex-metric-label">Rate</span><span class="cex-metric-value">' + formatRate(e.exchangeRate) + '</span></div>';
        html += '<div class="cex-metric"><span class="cex-metric-label">Foreign Amount</span><span class="cex-metric-value">' + formatMoney(e.foreignAmount, e.foreignCurrencyCode) + '</span></div>';
        if (e.remarks) {
            html += '<div class="cex-metric cex-metric-full"><span class="cex-metric-label">Remarks</span><span class="cex-metric-value">' + escapeHtml(e.remarks) + '</span></div>';
        }
        html += '</div>';
        html += '<div class="cex-card-actions">';
        html += '<a href="/CurrencyExchange/Edit/' + e.id + '" class="btn btn-sm btn-outline-primary"><i class="bi bi-pencil"></i> Edit</a>';
        if (exchangeState.isAdmin) {
            html += '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-exchange" data-id="' + e.id + '"><i class="bi bi-trash"></i> Delete</button>';
        }
        html += '</div>';
        html += '</div>';
    });

    $mobile.html(html);
}

function renderPagination(p) {
    var $container = $('#exchangePaginationContainer');
    $container.empty();
    if (!p || p.pageCount <= 1) return;

    var html = '<nav><ul class="pagination justify-content-center flex-wrap">';
    html += '<li class="page-item' + (p.hasPreviousPage ? '' : ' disabled') + '">' +
        '<a class="page-link" href="?page=' + (p.pageNumber - 1) + '">Previous</a></li>';

    for (var i = 1; i <= p.pageCount; i++) {
        html += '<li class="page-item' + (i === p.pageNumber ? ' active' : '') + '">' +
            '<a class="page-link" href="?page=' + i + '">' + i + '</a></li>';
    }

    html += '<li class="page-item' + (p.hasNextPage ? '' : ' disabled') + '">' +
        '<a class="page-link" href="?page=' + (p.pageNumber + 1) + '">Next</a></li>';
    html += '</ul></nav>';
    $container.html(html);
}

function renderTotalLkr(total) {
    if (total !== undefined && total !== null) {
        $('#exchangeTotalLkr').text(formatMoney(total, 'LKR'));
    }
}

function renderError(message) {
    $('#exchangeTableBody').html('<tr><td colspan="9" class="text-center text-danger py-4">' + escapeHtml(message) + '</td></tr>');
    $('#exchangeTableMobile').html('<div class="alert alert-danger">' + escapeHtml(message) + '</div>');
    $('#exchangeTableDesktop').css('display', '');
    $('#exchangeTableMobile').css('display', '');
}

function deleteExchange(id) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/CurrencyExchange/Delete',
        type: 'POST',
        data: { id: id, __RequestVerificationToken: token },
        success: function (response) {
            if (response.success) {
                if (typeof showToastSuccess === 'function') {
                    showToastSuccess(response.message || 'Currency exchange deleted successfully.');
                }
                loadExchanges();
            } else if (typeof showToastError === 'function') {
                showToastError(response.message || 'Delete failed.');
            }
        },
        error: function (xhr) {
            var msg = (xhr.responseJSON && xhr.responseJSON.message) || 'Delete failed.';
            if (typeof showToastError === 'function') {
                showToastError(msg);
            }
        }
    });
}
