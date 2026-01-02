/**
 * Expense Index Page JavaScript
 * Handles AJAX-based filtering, pagination, and delete functionality
 */

// Global state
var expenseState = {
    currentPage: 1,
    expenseTypeId: 0,
    payeeName: null,
    startDate: null,
    endDate: null,
    isAdmin: false,
    isInternalCallScope: false
};

// Safely read the admin flag from the hidden field and normalize to a boolean
function getIsAdminFlagForExpenses() {
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
    expenseState.isAdmin = getIsAdminFlagForExpenses();

    // Filter form submission
    $('#expenseFilterForm').on('submit', function (e) {
        e.preventDefault();
        expenseState.currentPage = 1;
        loadExpenses();
    });

    // Pagination clicks (delegated)
    $(document).on('click', '.pagination .page-link', function (e) {
        e.preventDefault();
        var href = $(this).attr('href');
        if (href && href !== '#') {
            var pageMatch = href.match(/page=(\d+)/);
            if (pageMatch) {
                expenseState.currentPage = parseInt(pageMatch[1]);
                loadExpenses();
            }
        }
    });

    // Delete expense handler
    $(document).on('click', '.btn-delete-expense', function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        var expenseId = $(this).data('expense-id');
        var expenseNo = $(this).data('expense-no');

        showConfirmDialog(
            'Are you sure you want to delete Expense #' + expenseNo + '?',
            function (confirmed) {
                if (confirmed) {
                    deleteExpense(expenseId);
                }
            },
            {
                type: 'Danger',
                title: 'Delete Expense',
                yesButtonText: 'Yes, Delete',
                noButtonText: 'No',
                warningText: 'This action cannot be undone.',
                dialogId: 'deleteExpenseConfirmDialog'
            }
        );

        return false;
    });

    // Filter change handlers
    $('#expenseTypeId').on('change', function () {
        expenseState.expenseTypeId = parseInt($('#expenseTypeId').val()) || 0;
        expenseState.currentPage = 1;
        loadExpenses();
    });

    // Payee filter with debounce for better performance
    var payeeTimeout;
    $('#payeeName').on('input', function () {
        clearTimeout(payeeTimeout);
        var self = this;
        payeeTimeout = setTimeout(function () {
            expenseState.payeeName = $(self).val().trim() || null;
            expenseState.currentPage = 1;
            loadExpenses();
        }, 500); // Wait 500ms after user stops typing
    });

    $('#startDate, #endDate').on('change', function () {
        if (!expenseState.isInternalCallScope) {
            expenseState.startDate = $('#startDate').val() || null;
            expenseState.endDate = $('#endDate').val() || null;
            // Remove active state from all date buttons when dates are manually changed
            $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
            expenseState.currentPage = 1;
            loadExpenses();
        }
    });

    // Helper function to format date as YYYY-MM-DD using local time
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

    //Load initial data(current month)
    $('#btnMonth').click();
});

function onDateBtnAction(fromDate, toDate, ele) {
    expenseState.isInternalCallScope = true;
    $('#startDate').val(fromDate);
    $('#endDate').val(toDate);
    expenseState.isInternalCallScope = false;

    expenseState.startDate = fromDate;
    expenseState.endDate = toDate;
    expenseState.currentPage = 1;

    $('.btn', $("#expenseFilterForm")).removeClass("active")
    $(ele).addClass("active");
    loadExpenses();
}

/**
 * Load expenses via AJAX
 */
function loadExpenses() {
    // Update state from form
    expenseState.expenseTypeId = parseInt($('#expenseTypeId').val()) || 0;
    expenseState.payeeName = $('#payeeName').val().trim() || null;

    // Show loading indicator
    $('#expenseLoadingIndicator').show();
    $('#expenseTableDesktop').hide();
    $('#expenseTableMobile').hide();
    $('#expensePaginationContainer').hide();

    // Build request data
    var requestData = {
        page: expenseState.currentPage,
        expenseTypeId: expenseState.expenseTypeId,
        payeeName: expenseState.payeeName,
        startDate: expenseState.startDate,
        endDate: expenseState.endDate
    };

    $.ajax({
        url: '/Expenses/GetExpenses',
        type: 'GET',
        data: requestData,
        success: function (response) {
            if (response.success) {
                renderExpenses(response.expenses);
                renderPagination(response.pagination);
            }
            else {
                showError('Failed to load expenses.');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading expenses:', error);
            showError('Error loading expenses. Please try again.');
        },
        complete: function () {
            $('#expenseLoadingIndicator').hide();
        }
    });
}

/**
 * Render expenses (desktop and mobile)
 */
function renderExpenses(expenses) {
    if (!expenses || expenses.length === 0) {
        renderEmptyState();
        return;
    }

    // Render desktop table
    renderDesktopTable(expenses);

    // Render mobile cards
    renderMobileCards(expenses);

    // Remove inline display styles to let CSS media queries handle visibility
    $('#expenseTableDesktop').css('display', '');
    $('#expenseTableMobile').css('display', '');
}

/**
 * Render desktop table
 */
function renderDesktopTable(expenses) {
    var html = '';

    expenses.forEach(function (expense) {
        html += '<tr>';
        html += '<td>' + escapeHtml(expense.id) + '</td>';
        html += '<td>' + formatDate(expense.date) + '</td>';
        html += '<td>' + escapeHtml(expense.expenseTypeName) + '</td>';
        html += '<td>' + escapeHtml(expense.payeeName) + '</td>';
        html += '<td class="text-end fw-bold">' + formatCurrency(expense.amount) + '</td>';
        html += '<td>' + escapeHtml(expense.paymentMethodDisplay) + '</td>';
        html += '<td>';
        html += '<div class="d-flex gap-1">';
        html += '<a href="/Expenses/Details/' + expense.id + '" class="btn btn-sm btn-info">View</a>';
        html += '<a href="/Expenses/Edit/' + expense.id + '" class="btn btn-sm btn-warning">Edit</a>';
        if (expenseState.isAdmin === true) {
            html += '<button type="button" class="btn btn-sm btn-danger btn-delete-expense" ';
            html += 'data-expense-id="' + expense.id + '" data-expense-no="' + expense.id + '">';
            html += 'Delete</button>';
        }
        html += '</div>';
        html += '</td>';
        html += '</tr>';
    });

    $('#expenseTableBody').html(html);
}

/**
 * Render mobile cards
 */
function renderMobileCards(expenses) {
    // Group expenses by date
    var grouped = {};
    expenses.forEach(function (expense) {
        var dateKey = expense.date;
        if (!grouped[dateKey]) {
            grouped[dateKey] = [];
        }
        grouped[dateKey].push(expense);
    });

    // Sort dates descending
    var sortedDates = Object.keys(grouped).sort(function (a, b) {
        return new Date(b) - new Date(a);
    });

    var html = '';

    sortedDates.forEach(function (date) {
        var dateExpenses = grouped[date];
        var dateDisplay = formatDateDisplay(date);

        // Calculate total for this date
        var dateTotal = dateExpenses.reduce(function (sum, expense) {
            return sum + parseFloat(expense.amount || 0);
        }, 0);

        html += '<div class="expense-date-group">';
        html += '<div class="expense-date-header">';
        html += '<h6 class="expense-date-title">' + dateDisplay + '</h6>';
        html += '<small class="expense-date-total">Total: ' + formatCurrency(dateTotal) + '</small>';
        html += '</div>';

        dateExpenses.forEach(function (expense) {
            var expenseTypeName = expense.payeeName || expense.expenseTypeName || 'Expense';

            html += '<div class="expense-row-wrapper">';
            html += '<div class="expense-row-compact expense-status-default">';
            html += '<div class="expense-row-header">';
            html += '<div class="expense-type-icon"><i class="bi bi-cash-stack"></i></div>';
            html += '<div class="expense-payee-name">';
            html += '<span class="expense-number-label">EXP No</span>';
            html += '<span class="expense-number-value">#' + expense.id + '</span> - ';
            html += escapeHtml(expenseTypeName);
            html += '</div>';
            html += '<div class="expense-amount">' + formatCurrency(expense.amount) + '</div>';
            html += '</div>';
            html += '<div class="expense-row-footer">';
            html += '<span class="expense-number">';
            if (expenseState.isAdmin === true) {
                html += '<button type="button" class="btn-delete-expense-icon btn-delete-expense" ';
                html += 'data-expense-id="' + expense.id + '" data-expense-no="' + expense.id + '" ';
                html += 'title="Delete Expense"><i class="bi bi-trash"></i></button>';
            }
            html += '</span>';
            html += '<span class="expense-type-badge">' + escapeHtml(expense.expenseTypeName || '-') + '</span>';
            html += '<span class="expense-payment-method">' + escapeHtml(expense.paymentMethodDisplay) + '</span>';
            html += '<a href="/Expenses/Edit/' + expense.id + '" class="btn-view-expense">';
            html += '<i class="bi bi-eye"></i> View</a>';
            if (expense.createdByMember && expense.createdByMember.name) {
                html += '<span class="expense-creator">' + escapeHtml(expense.createdByMember.name) + '</span>';
            }
            html += '</div>';
            html += '</div>';
            html += '</div>';
        });

        html += '</div>';
    });

    $('#expenseTableMobile').html(html);
}

/**
 * Render pagination
 */
function renderPagination(pagination) {
    if (!pagination || pagination.pageCount <= 1) {
        $('#expensePaginationContainer').html('');
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
    html += pagination.totalItemCount + ' expenses';
    html += '</small></div>';

    $('#expensePaginationContainer').html(html).show();
}

/**
 * Render empty state
 */
function renderEmptyState() {
    var emptyHtml = '<div class="text-center text-muted py-5">';
    emptyHtml += '<i class="bi bi-inbox fs-1"></i>';
    emptyHtml += '<p class="mt-3 fs-5">No expenses found.</p>';
    emptyHtml += '</div>';

    $('#expenseTableBody').html('<tr><td colspan="7" class="text-center text-muted">No expenses found.</td></tr>');
    $('#expenseTableMobile').html(emptyHtml);
    $('#expenseTableDesktop').css('display', '');
    $('#expenseTableMobile').css('display', '');
}

/**
 * Delete expense via AJAX
 */
function deleteExpense(expenseId) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    var deleteUrl = '/Expenses/Delete/' + expenseId;

    $.ajax({
        url: deleteUrl,
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                if (typeof showToastSuccess === 'function') {
                    showToastSuccess(response.message || 'Expense deleted successfully.');
                } else {
                    alert(response.message || 'Expense deleted successfully.');
                }
                loadExpenses(); // Reload expenses
            } else {
                if (typeof showToastError === 'function') {
                    showToastError(response.message || 'Error deleting expense.');
                } else {
                    alert(response.message || 'Error deleting expense.');
                }
            }
        },
        error: function (xhr, status, error) {
            var errorMessage = 'Error deleting expense. Please try again.';
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
    $('#expenseTableBody').html('<tr><td colspan="7">' + errorHtml + '</td></tr>');
    $('#expenseTableMobile').html('<div class="alert alert-danger">' + escapeHtml(message) + '</div>');
    $('#expenseTableDesktop').css('display', '');
    $('#expenseTableMobile').css('display', '');
}

