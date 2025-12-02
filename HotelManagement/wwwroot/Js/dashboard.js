// Dashboard metrics functionality
function formatCurrency(value) {
    const num = Number(value || 0);
    return 'LKR ' + num.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function loadMetrics(ele) {
    const from = $('#fromDate').val();
    const to = $('#toDate').val();

    $('.btn', $(".filter-section")).removeClass("btn-primary").addClass("btn-outline-secondary");
    ele.removeClass("btn-outline-secondary").addClass("btn-primary");

    $.getJSON('/Home/GetMetrics', { from: from, to: to }, function (res) {
        if (res && res.success && res.data) {
            //if (res.isAdmin && $('#tileTotalIncome').length) {
            //    $('#tileTotalIncome').text(formatCurrency(res.data.totalIncome));
            //}
            //if (res.isAdmin && $('#tileTotalRevenue').length) {
            //    $('#tileTotalRevenue').text(formatCurrency(res.data.totalRevenue));
            //}
            $('#tileTotalIncome').text(formatCurrency(res.data.totalIncome));
            $('#tileTotalRevenue').text(formatCurrency(res.data.totalRevenue));
            $('#tileExpenses').text(formatCurrency(res.data.totalExpenses));
            $('#tileRestaurantRevenue').text(formatCurrency(res.data.restaurantRevenue));
            $('#tileServiceCharges').text(formatCurrency(res.data.serviceCharges));
            $('#tileLaundryRevenue').text(formatCurrency(res.data.laundryRevenue));
            $('#tileTourRevenue').text(formatCurrency(res.data.tourRevenue));
            $('#tileStayRevenue').text(formatCurrency(res.data.stayRevenue));
            $('#tileOtherRevenue').text(formatCurrency(res.data.otherRevenue));
        }
    });
}

// Dashboard tile click handler - defined outside InitializeDashboard to ensure it's available
function handleDashboardTileClick() {
    $('.dashboard-tile').off('click').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        
        const $tile = $(this);
        const tileType = $tile.data('tile-type');
        const tileTitle = $tile.data('tile-title') || 'Details';
        const from = $('#fromDate').val();
        const to = $('#toDate').val();

        if (!tileType) {
            console.error('Tile type not found on element:', $tile);
            return;
        }

        console.log('Opening modal for tile type:', tileType);

        $('#dashboardTileModalLabel').text(tileTitle);
        $('#dashboardTileModalBody').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div><p class="mt-3">Loading data...</p></div>');
        
        const modalElement = document.getElementById('dashboardTileModal');
        if (!modalElement) {
            console.error('Modal element not found');
            alert('Modal element not found. Please refresh the page.');
            return;
        }

        // Check if bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('Bootstrap is not available');
            alert('Bootstrap is not loaded. Please refresh the page.');
            return;
        }

        const modal = new bootstrap.Modal(modalElement);
        modal.show();

        $.getJSON('/Home/GetTileData', { tileType: tileType, from: from, to: to }, function (res) {
            if (res && res.success && res.data) {
                renderTileData(tileType, res.data, res.isAdmin);
            } else {
                $('#dashboardTileModalBody').html('<div class="alert alert-danger">Error loading data.</div>');
            }
        }).fail(function (xhr, status, error) {
            console.error('Error loading tile data:', error, xhr);
            $('#dashboardTileModalBody').html('<div class="alert alert-danger">Error loading data. Please try again.</div>');
        });
    });
}

function InitializeDashboard() {
    $(function () {
        // Default to today
        const today = new Date().toISOString().slice(0, 10);
        $('#fromDate').val(today);
        $('#toDate').val(today);
        loadMetrics($('#btnApply', $(".filter-section")));

        $('#btnApply').on('click', function () {
            loadMetrics($('#btnApply', $(".filter-section")));
        });

        $('#btnToday').on('click', function () {
            const t = new Date().toISOString().slice(0, 10);
            $('#fromDate').val(t);
            $('#toDate').val(t);

            loadMetrics($('#btnToday', $(".filter-section")));
        });

        $('#btnYesterday').on('click', function () {
            var date = new Date();
            date.setDate(date.getDate() - 1);
            const t = date.toISOString().slice(0, 10);
            $('#fromDate').val(t);
            $('#toDate').val(t);

            loadMetrics($('#btnYesterday', $(".filter-section")));
        });

        $('#btnMonth').on('click', function () {
            const now = new Date();

            // Get first day of current month (local time)
            const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);

            // Format to YYYY-MM-DD in local time
            const formatDate = (d) =>
                d.getFullYear() + '-' +
                String(d.getMonth() + 1).padStart(2, '0') + '-' +
                String(d.getDate()).padStart(2, '0');

            $('#fromDate').val(formatDate(firstDay));
            $('#toDate').val(formatDate(now));

            loadMetrics($('#btnMonth', $(".filter-section")));
        });

        $('#btnYear').on('click', function () {
            const now = new Date();

            // Get January 1st of the current year
            const firstDayOfYear = new Date(now.getFullYear(), 0, 1);

            // Format date to YYYY-MM-DD (local)
            const formatDate = (d) =>
                d.getFullYear() + '-' +
                String(d.getMonth() + 1).padStart(2, '0') + '-' +
                String(d.getDate()).padStart(2, '0');

            $('#fromDate').val(formatDate(firstDayOfYear));
            $('#toDate').val(formatDate(now));

            loadMetrics($('#btnYear', $(".filter-section")));
        });

        // Attach dashboard tile click handlers
        handleDashboardTileClick();
    });
}

function renderTileData(tileType, data, isAdmin) {
    let html = '<div class="container-fluid py-3">';
    
    if (tileType === 'expenses') {
        html += renderExpensesMobile(data, isAdmin);
    } else {
        html += renderInvoicesMobile(data, isAdmin, tileType);
    }
    
    html += '</div>';
    $('#dashboardTileModalBody').html(html);
}

function renderInvoicesMobile(data, isAdmin, tileType) {
    if (!data || data.length === 0) {
        return '<div class="text-center text-muted py-5"><i class="bi bi-inbox fs-1"></i><p class="mt-3 fs-5">No invoices found.</p></div>';
    }

    // Group by date
    const grouped = {};
    data.forEach(item => {
        const date = new Date(item.date);
        const dateKey = date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: '2-digit' });
        if (!grouped[dateKey]) grouped[dateKey] = [];
        grouped[dateKey].push(item);
    });

    // Sort dates descending
    const sortedDates = Object.keys(grouped).sort((a, b) => {
        const dateA = new Date(a);
        const dateB = new Date(b);
        return dateB - dateA;
    });

    let html = '<div class="invoice-table-mobile">';
    
    sortedDates.forEach(dateKey => {
        // Calculate daily total for this date
        const dateTotal = grouped[dateKey].reduce((sum, invoice) => {
            return sum + parseFloat(invoice.amount || 0);
        }, 0);
        
        html += `<div class="invoice-date-group">
            <div class="invoice-date-header">
                <h6 class="invoice-date-title">${dateKey}</h6>
                <small class="invoice-date-total">Total: ${formatCurrency(dateTotal)}</small>
            </div>`;

        grouped[dateKey].forEach(invoice => {
            const statusClass = getInvoiceStatusClass(invoice.status);
            const typeIcon = getInvoiceTypeIcon(invoice.type);
            const customerName = invoice.customerName || 'Unknown Customer';

            html += `<div class="invoice-row-wrapper">
                <div class="invoice-row-compact ${statusClass}">
                    <div class="invoice-row-header">
                        <div class="invoice-type-icon">
                            <i class="bi ${typeIcon}"></i>
                        </div>
                        <div class="invoice-customer-name">${customerName}</div>
                        <div class="invoice-amount">${formatCurrency(invoice.amount)}</div>
                    </div>
                    <div class="invoice-row-footer">
                        <span class="invoice-number">
                            <span class="invoice-number-label">INV No</span>
                            <span class="invoice-number-value">#${invoice.id}</span>
                            ${isAdmin ? `<button type="button" class="btn-delete-invoice-icon btn-delete-invoice" data-invoice-id="${invoice.id}" data-invoice-no="${invoice.id}" title="Delete Invoice"><i class="bi bi-trash"></i></button>` : ''}
                        </span>
                        <a href="/Internal/Invoices/Edit/${invoice.id}" class="btn-view-invoice" target="_Blank">
                            <i class="bi bi-eye"></i> View
                        </a>
                    </div>
                </div>
            </div>`;
        });

        html += '</div>';
    });

    html += '</div>';
    return html;
}

function renderExpensesMobile(data, isAdmin) {
    if (!data || data.length === 0) {
        return '<div class="text-center text-muted py-5"><i class="bi bi-inbox fs-1"></i><p class="mt-3 fs-5">No expenses found.</p></div>';
    }

    // Group by date
    const grouped = {};
    data.forEach(item => {
        const date = new Date(item.date);
        const dateKey = date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: '2-digit' });
        if (!grouped[dateKey]) grouped[dateKey] = [];
        grouped[dateKey].push(item);
    });

    // Sort dates descending
    const sortedDates = Object.keys(grouped).sort((a, b) => {
        const dateA = new Date(a);
        const dateB = new Date(b);
        return dateB - dateA;
    });

    let html = '<div class="expenses-table-mobile">';
    
    sortedDates.forEach(dateKey => {
        // Calculate daily total for this date
        const dateTotal = grouped[dateKey].reduce((sum, expense) => {
            return sum + parseFloat(expense.amount || 0);
        }, 0);
        
        html += `<div class="expense-date-group">
            <div class="expense-date-header">
                <h6 class="expense-date-title">${dateKey}</h6>
                <small class="expense-date-total">Total: ${formatCurrency(dateTotal)}</small>
            </div>`;

        grouped[dateKey].forEach(expense => {
            const expenseTypeName = expense.payeeName || 'Expense';

            html += `<div class="expense-row-wrapper">
                <div class="expense-row-compact expense-status-default">
                    <div class="expense-row-header">
                        <div class="expense-type-icon">
                            <i class="bi bi-cash-stack"></i>
                        </div>
                        <div class="expense-payee-name">
                            <span class="expense-number-label">EXP No</span>
                            <span class="expense-number-value">#${expense.id}</span> - ${expenseTypeName}
                        </div>
                        <div class="expense-amount">${formatCurrency(expense.amount)}</div>
                    </div>
                    <div class="expense-row-footer">
                        <span class="expense-number">
                            ${isAdmin ? `<button type="button" class="btn-delete-expense-icon btn-delete-expense" data-expense-id="${expense.id}" data-expense-no="${expense.id}" title="Delete Expense"><i class="bi bi-trash"></i></button>` : ''}
                        </span>
                        <span class="expense-type-badge">${expense.expenseTypeName || '-'}</span>
                        <span class="expense-payment-method">${expense.paymentMethod}</span>
                        <a href="/Expenses/Edit/${expense.id}" class="btn-view-expense">
                            <i class="bi bi-eye"></i> View
                        </a>
                    </div>
                </div>
            </div>`;
        });

        html += '</div>';
    });

    html += '</div>';
    return html;
}

function getInvoiceStatusClass(status) {
    const statusMap = {
        'InProgress': 'status-in-progress',
        'Complete': 'status-complete',
        'PartiallyPaid': 'status-partially-paid',
        'Paid': 'status-paid'
    };
    return statusMap[status] || 'bg-secondary';
}

function getInvoiceTypeIcon(type) {
    const iconMap = {
        'Dining': 'bi-cup-hot-fill',
        'TakeAway': 'bi-bag-check-fill',
        'Stay': 'bi-building-check',
        'Tour': 'bi-bus-front-fill',
        'Laundry': 'bi-basket2-fill',
        'Other': 'bi-gear-fill'
    };
    return iconMap[type] || 'bi-receipt';
}

