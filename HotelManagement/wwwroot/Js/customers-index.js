/**
 * Customers Index Page JavaScript
 * AJAX-based loading, status tabs, and pagination
 */

var customerState = {
    currentPage: 1,
    status: 'all'
};

$(document).ready(function () {
    // Tab clicks
    $('#customerStatusTabs [data-filter]').on('click', function () {
        $('#customerStatusTabs [data-filter]').removeClass('active');
        $(this).addClass('active');

        customerState.status = $(this).data('filter') || 'all';
        customerState.currentPage = 1;
        loadCustomers();
    });

    // Pagination (delegated)
    $(document).on('click', '#customerPaginationContainer .pagination .page-link', function (e) {
        e.preventDefault();
        var href = $(this).attr('href');
        if (href && href !== '#') {
            var pageMatch = href.match(/page=(\d+)/);
            if (pageMatch) {
                customerState.currentPage = parseInt(pageMatch[1]);
                loadCustomers();
            }
        }
    });

    // Initial load (all customers)
    loadCustomers();
});

function loadCustomers() {
    var requestData = {
        page: customerState.currentPage,
        status: customerState.status
    };

    $.ajax({
        url: '/Customers/GetCustomers',
        type: 'GET',
        data: requestData,
        success: function (response) {
            if (response && response.success) {
                renderCustomers(response.customers || []);
                renderCustomerPagination(response.pagination);
            } else {
                showCustomerError('Failed to load customers.');
            }
        },
        error: function () {
            showCustomerError('Error loading customers. Please try again.');
        }
    });
}

function renderCustomers(customers) {
    if (!customers || customers.length === 0) {
        var emptyHtml = '<div class="text-center text-muted py-5">' +
            '<i class="bi bi-inbox fs-1"></i>' +
            '<p class="mt-3 fs-5">No customers found.</p>' +
            '</div>';

        $('#customerTableBody').html('<tr><td colspan="7" class="text-center text-muted">No customers found.</td></tr>');
        $('#customerTableMobile').html(emptyHtml);
        return;
    }

    renderCustomerDesktop(customers);
    renderCustomerMobile(customers);
}

function renderCustomerDesktop(customers) {
    var html = '';

    customers.forEach(function (c) {
        var fullName = (c.firstName + ' ' + c.lastName).trim();
        var statusText = c.active ? 'Active' : 'Inactive';
        var statusClass = c.active ? 'text-success' : 'text-muted';
        var roomDisplay = c.roomNo && c.roomNo.trim() !== '' ? '#' + c.roomNo : '-';

        html += '<tr>';
        html += '<td>' + escapeHtml(c.email) + '</td>';
        html += '<td>' + escapeHtml(fullName) + '</td>';
        html += '<td>' + escapeHtml(c.contactNo) + '</td>';
        html += '<td class="' + statusClass + ' fw-semibold">' + statusText + '</td>';
        html += '<td>' + escapeHtml(c.passportNo) + '</td>';
        html += '<td>' + escapeHtml(roomDisplay) + '</td>';
        html += '<td><a class="btn btn-sm btn-warning me-2" href="/Customers/Edit/' + c.id + '">Edit</a></td>';
        html += '</tr>';
    });

    $('#customerTableBody').html(html);
}

function renderCustomerMobile(customers) {
    var html = '';

    customers.forEach(function (c) {
        var fullName = (c.firstName + ' ' + c.lastName).trim();
        var statusText = c.active ? 'Active' : 'Inactive';
        var badgeClass = c.active ? 'bg-success' : 'bg-secondary';
        var roomDisplay = c.roomNo && c.roomNo.trim() !== '' ? '#' + c.roomNo : '';

        html += '<div class="list-card">';
        html += '<div class="list-card-header d-flex justify-content-between align-items-center">';
        html += '<h6 class="list-card-title mb-0">';
        if (roomDisplay) {
            html += escapeHtml(roomDisplay + ' - ' + fullName);
        } else {
            html += escapeHtml(fullName);
        }
        html += '</h6>';
        html += '<span class="badge ' + badgeClass + '">' + statusText + '</span>';
        html += '</div>';

        html += '<div class="list-card-body">';
        html += '<div class="list-card-row full-width">';
        html += '<div class="list-card-label">Email</div>';
        html += '<div class="list-card-value">' + escapeHtml(c.email) + '</div>';
        html += '</div>';

        html += '<div class="list-card-row">';
        html += '<div class="list-card-label">Contact</div>';
        html += '<div class="list-card-value">' + escapeHtml(c.contactNo) + '</div>';
        html += '</div>';

        html += '<div class="list-card-row">';
        html += '<div class="list-card-label">Passport No</div>';
        html += '<div class="list-card-value">' + escapeHtml(c.passportNo) + '</div>';
        html += '</div>';

        if (roomDisplay) {
            html += '<div class="list-card-row">';
            html += '<div class="list-card-label">Room No</div>';
            html += '<div class="list-card-value">' + escapeHtml(roomDisplay) + '</div>';
            html += '</div>';
        }

        html += '</div>'; // body

        html += '<div class="list-card-footer">';
        html += '<a href="/Customers/Edit/' + c.id + '" class="btn btn-warning btn-sm">';
        html += '<i class="bi bi-pencil-square"></i> Edit</a>';
        html += '</div>';
        html += '</div>'; // card
    });

    $('#customerTableMobile').html(html);
}

function renderCustomerPagination(pagination) {
    if (!pagination || pagination.pageCount <= 1) {
        $('#customerPaginationContainer').html('');
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
    html += '<div class="text-center mt-2"><small class="text-muted">';
    html += 'Showing ' + ((pagination.pageNumber - 1) * customerState.pageSize + 1 || 1) + ' to ';
    html += Math.min(pagination.pageNumber * customerState.pageSize || pagination.totalItemCount, pagination.totalItemCount) + ' of ';
    html += pagination.totalItemCount + ' customers';
    html += '</small></div>';

    $('#customerPaginationContainer').html(html);
}

function showCustomerError(message) {
    var errorHtml = '<div class="alert alert-danger">' + escapeHtml(message) + '</div>';
    $('#customerTableBody').html('<tr><td colspan="7">' + errorHtml + '</td></tr>');
    $('#customerTableMobile').html(errorHtml);
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


