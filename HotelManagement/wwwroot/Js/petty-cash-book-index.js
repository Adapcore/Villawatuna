/**
 * Petty Cash Book Index — list, filters, pagination, delete
 * Desktop table + mobile cards
 */
var pettyCashState = {
    currentPage: 1,
    startDate: null,
    endDate: null,
    isInternalCallScope: false,
    isAdmin: false
};

function formatDateLocal(date) {
    var year = date.getFullYear();
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var day = String(date.getDate()).padStart(2, '0');
    return year + '-' + month + '-' + day;
}

function formatMoney(value) {
    var n = parseFloat(value);
    if (isNaN(n)) n = 0;
    return 'Rs. ' + n.toFixed(2);
}

function formatDateDisplay(dateStr) {
    if (!dateStr) return '';
    var d = new Date(dateStr + 'T00:00:00');
    if (isNaN(d.getTime())) return dateStr;
    var opts = { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' };
    return d.toLocaleDateString(undefined, opts);
}

function getIsAdminFlag() {
    var val = $('#isAdminFlag').data('is-admin');
    if (typeof val === 'string') {
        return val.toLowerCase() === 'true';
    }
    return !!val;
}

$(document).ready(function () {
    pettyCashState.isAdmin = getIsAdminFlag();

    $('#pettyCashFilterForm').on('submit', function (e) {
        e.preventDefault();
        pettyCashState.currentPage = 1;
        loadEntries();
    });

    $(document).on('click', '.pagination .page-link', function (e) {
        e.preventDefault();
        var href = $(this).attr('href');
        if (href && href !== '#') {
            var pageMatch = href.match(/page=(\d+)/);
            if (pageMatch) {
                pettyCashState.currentPage = parseInt(pageMatch[1]);
                loadEntries();
            }
        }
    });

    $('#startDate, #endDate').on('change', function () {
        if (!pettyCashState.isInternalCallScope) {
            pettyCashState.startDate = $('#startDate').val() || null;
            pettyCashState.endDate = $('#endDate').val() || null;
            $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
            pettyCashState.currentPage = 1;
            loadEntries();
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
        pettyCashState.isInternalCallScope = true;
        $('#startDate').val('');
        $('#endDate').val('');
        pettyCashState.isInternalCallScope = false;
        pettyCashState.startDate = null;
        pettyCashState.endDate = null;
        pettyCashState.currentPage = 1;
        $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
        loadEntries();
    });

    $(document).on('click', '.btn-delete-petty-cash', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var date = $(this).data('date');

        showConfirmDialog(
            'Are you sure you want to delete the petty cash entry for ' + date + '?',
            function (confirmed) {
                if (confirmed) {
                    deleteEntry(id);
                }
            },
            {
                type: 'Danger',
                title: 'Delete Entry',
                yesButtonText: 'Yes, Delete',
                noButtonText: 'No',
                warningText: 'This action cannot be undone.',
                dialogId: 'deletePettyCashConfirmDialog'
            }
        );
    });

    $('#btnMonth').click();
});

function onDateBtnAction(fromDate, toDate, ele) {
    pettyCashState.isInternalCallScope = true;
    $('#startDate').val(fromDate);
    $('#endDate').val(toDate);
    pettyCashState.isInternalCallScope = false;

    pettyCashState.startDate = fromDate;
    pettyCashState.endDate = toDate;
    pettyCashState.currentPage = 1;

    $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
    $(ele).addClass('active');
    loadEntries();
}

function loadEntries() {
    $('#pettyCashLoadingIndicator').show();
    $('#pettyCashTableDesktop').hide();
    $('#pettyCashTableMobile').hide();
    $('#pettyCashPaginationContainer').hide();

    $.ajax({
        url: '/PettyCashBook/GetEntries',
        type: 'GET',
        data: {
            page: pettyCashState.currentPage,
            startDate: pettyCashState.startDate,
            endDate: pettyCashState.endDate
        },
        success: function (response) {
            $('#pettyCashLoadingIndicator').hide();
            if (!response.success) {
                if (typeof showToast === 'function') {
                    showToast('Failed to load entries.', 'error');
                }
                return;
            }
            renderEntries(response.entries || []);
            renderPagination(response.pagination);
            // Let CSS media queries control desktop vs mobile visibility
            $('#pettyCashTableDesktop').css('display', '');
            $('#pettyCashTableMobile').css('display', '');
            $('#pettyCashPaginationContainer').show();
        },
        error: function () {
            $('#pettyCashLoadingIndicator').hide();
            $('#pettyCashTableDesktop').css('display', '');
            $('#pettyCashTableMobile').css('display', '');
            if (typeof showToast === 'function') {
                showToast('Error loading petty cash entries.', 'error');
            }
        }
    });
}

function renderEntries(entries) {
    renderDesktopTable(entries);
    renderMobileCards(entries);
}

function renderDesktopTable(entries) {
    var $tbody = $('#pettyCashTableBody');
    $tbody.empty();

    if (!entries.length) {
        $tbody.append('<tr><td colspan="10" class="text-center text-muted py-4">No entries found.</td></tr>');
        return;
    }

    entries.forEach(function (e) {
        var row = '<tr>' +
            '<td>' + e.date + '</td>' +
            '<td class="text-end">' + formatMoney(e.openingBalance) + '</td>' +
            '<td class="text-end">' + formatMoney(e.pettyCashRefillOpening) + '</td>' +
            '<td class="text-end">' + formatMoney(e.totalIncomeBaseCurry) + '</td>' +
            '<td class="text-end">' + formatMoney(e.totalExpences) + '</td>' +
            '<td class="text-end">' + formatMoney(e.balance) + '</td>' +
            '<td class="text-end">' + formatMoney(e.handedOver) + '</td>' +
            '<td class="text-end">' + formatMoney(e.pettyCashRefillClosing) + '</td>' +
            '<td class="text-end">' + formatMoney(e.closeBalance) + '</td>' +
            '<td class="text-center">' +
            '<a href="/PettyCashBook/Edit/' + e.id + '" class="btn btn-sm btn-outline-primary me-1" title="Edit"><i class="bi bi-pencil"></i></a>' +
            (pettyCashState.isAdmin
                ? '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-petty-cash" data-id="' + e.id + '" data-date="' + e.date + '" title="Delete"><i class="bi bi-trash"></i></button>'
                : '') +
            '</td>' +
            '</tr>';
        $tbody.append(row);
    });
}

function renderMobileCards(entries) {
    var $mobile = $('#pettyCashTableMobile');
    $mobile.empty();

    if (!entries.length) {
        $mobile.html('<div class="pcb-empty">No entries found.</div>');
        return;
    }

    var html = '';
    entries.forEach(function (e) {
        html += '<div class="pcb-card">';
        html += '<div class="pcb-card-header">';
        html += '<h6 class="pcb-card-date">' + formatDateDisplay(e.date) + '</h6>';
        html += '<span class="pcb-card-close">' + formatMoney(e.closeBalance) + '</span>';
        html += '</div>';

        html += '<div class="pcb-card-grid">';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Opening</span><span class="pcb-metric-value">' + formatMoney(e.openingBalance) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Refill (Opening)</span><span class="pcb-metric-value">' + formatMoney(e.pettyCashRefillOpening) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Income (LKR)</span><span class="pcb-metric-value is-income">' + formatMoney(e.totalIncomeBaseCurry) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Expenses</span><span class="pcb-metric-value is-expense">' + formatMoney(e.totalExpences) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Daily Balance</span><span class="pcb-metric-value">' + formatMoney(e.balance) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Handed Over</span><span class="pcb-metric-value">' + formatMoney(e.handedOver) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Refill (Closing)</span><span class="pcb-metric-value">' + formatMoney(e.pettyCashRefillClosing) + '</span></div>';
        html += '<div class="pcb-metric"><span class="pcb-metric-label">Closing</span><span class="pcb-metric-value">' + formatMoney(e.closeBalance) + '</span></div>';
        html += '</div>';

        html += '<div class="pcb-card-actions">';
        html += '<a href="/PettyCashBook/Edit/' + e.id + '" class="btn btn-sm btn-outline-primary"><i class="bi bi-pencil"></i> Edit</a>';
        if (pettyCashState.isAdmin) {
            html += '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-petty-cash" data-id="' + e.id + '" data-date="' + e.date + '"><i class="bi bi-trash"></i> Delete</button>';
        }
        html += '</div>';
        html += '</div>';
    });

    $mobile.html(html);
}

function renderPagination(p) {
    var $container = $('#pettyCashPaginationContainer');
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

function deleteEntry(id) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/PettyCashBook/Delete',
        type: 'POST',
        data: { id: id, __RequestVerificationToken: token },
        success: function (response) {
            if (response.success) {
                if (typeof showToast === 'function') {
                    showToast(response.message || 'Deleted.', 'success');
                }
                loadEntries();
            } else if (typeof showToast === 'function') {
                showToast(response.message || 'Delete failed.', 'error');
            }
        },
        error: function (xhr) {
            var msg = (xhr.responseJSON && xhr.responseJSON.message) || 'Delete failed.';
            if (typeof showToast === 'function') {
                showToast(msg, 'error');
            }
        }
    });
}
