// Item Wise Sales Report JavaScript

var itemSalesState = {
    fromDate: null,
    toDate: null,
    itemId: 0,
    category: 'All',
    page: 1,
    pageSize: 50
};

$(document).ready(function () {
    // Load item options (same source as invoice details - /api/menu/getItems)
    loadItemOptions();

    // Category change
    $('#categoryFilter').on('change', function () {
        itemSalesState.category = $('#categoryFilter').val() || 'All';
        // Reset selected item when category changes
        $('#itemId').val('0').trigger('change');
        // Reload item options and data for the selected category
        itemSalesState.page = 1;
        loadItemOptions();
        loadItemSales();
    });

    // Open item search popup
    $('#btnOpenItemSearch').on('click', function () {
        openReportItemSearchPopup();
    });

    // Filter form submission
    $('#itemSalesFilterForm').on('submit', function (e) {
        e.preventDefault();
        itemSalesState.itemId = parseInt($('#itemId').val()) || 0;
        itemSalesState.fromDate = $('#fromDate').val() || null;
        itemSalesState.toDate = $('#toDate').val() || null;
        itemSalesState.category = $('#categoryFilter').val() || 'All';
        itemSalesState.page = 1;
        loadItemSales();
    });

    // Refresh result when item changes
    $('#itemId').on('change', function () {
        itemSalesState.itemId = parseInt($(this).val()) || 0;
        itemSalesState.page = 1;
        loadItemSales();
    });

    // Refresh when dates are changed manually
    $('#fromDate, #toDate').on('change', function () {
        itemSalesState.fromDate = $('#fromDate').val() || null;
        itemSalesState.toDate = $('#toDate').val() || null;
        // Clear active state from quick buttons when user picks custom dates
        $('#btnToday, #btnYesterday, #btnMonth, #btnYear').removeClass('active');
        itemSalesState.page = 1;
        loadItemSales();
    });

    // Date helper
    var formatDateLocal = function (date) {
        var year = date.getFullYear();
        var month = String(date.getMonth() + 1).padStart(2, '0');
        var day = String(date.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    function onDateBtnAction(fromDate, toDate, ele) {
        $('#fromDate').val(fromDate);
        $('#toDate').val(toDate);

        itemSalesState.fromDate = fromDate;
        itemSalesState.toDate = toDate;

        $('.btn', $("#itemSalesFilterForm")).removeClass("active");
        $(ele).addClass("active");

        itemSalesState.page = 1;
        loadItemSales();
    }

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
        var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
        var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
        var firstDayStr = formatDateLocal(firstDay);
        var lastDayStr = formatDateLocal(lastDay);
        onDateBtnAction(firstDayStr, lastDayStr, this);
    });

    $('#btnYear').on('click', function () {
        var now = new Date();
        var firstDay = new Date(now.getFullYear(), 0, 1);
        var lastDay = new Date(now.getFullYear(), 11, 31);
        var firstDayStr = formatDateLocal(firstDay);
        var lastDayStr = formatDateLocal(lastDay);
        onDateBtnAction(firstDayStr, lastDayStr, this);
    });

    // Pagination click handler (delegated)
    $(document).on('click', '#itemSalesPagination .page-link', function (e) {
        e.preventDefault();
        var page = parseInt($(this).data('page'));
        if (!page || page === itemSalesState.page || page < 1) {
            return;
        }
        itemSalesState.page = page;
        loadItemSales();
    });

    // Print report
    $('#btnPrintItemSales').on('click', function () {
        window.print();
    });

    // Refresh button - reload with current filters
    $('#btnRefreshItemSales').on('click', function () {
        // Update state with current filter values
        itemSalesState.itemId = parseInt($('#itemId').val()) || 0;
        itemSalesState.fromDate = $('#fromDate').val() || null;
        itemSalesState.toDate = $('#toDate').val() || null;
        itemSalesState.category = $('#categoryFilter').val() || 'All';
        itemSalesState.page = 1; // Reset to first page on refresh
        loadItemSales();
    });

    // Initial load - current month
    $('#btnMonth').click();
});

function loadItemOptions() {
    var category = ($('#categoryFilter').val() || 'Restaurant').toLowerCase();
    var $select = $('#itemId');
    $select.empty();
    $select.append('<option value="0">-- All Items --</option>');

    // Helper to finalize Select2 and globals
    function finalize(groups) {
        window.reportItemGroups = groups || {};
        initItemSelect2();
    }

    // Restaurant (Dining / TakeAway) – same as invoice LoadItems
    if (category === 'restaurant') {
        $.getJSON('/api/menu/getItems', function (data) {
            if (!data) {
                finalize({});
                return;
            }

            var allItems = [];
            data.forEach(function (cat) {
                cat.items.forEach(function (itm) {
                    allItems.push({
                        id: itm.id,
                        name: itm.name,
                        price: itm.price,
                        category: cat.category
                    });
                });
            });

            var groups = {};
            allItems.forEach(function (i) {
                if (!groups[i.category]) groups[i.category] = [];
                groups[i.category].push(i);
            });

            // Fill dropdown with optgroups
            $.each(groups, function (catName, items) {
                $select.append('<optgroup label="' + escapeHtml(catName) + '"></optgroup>');
                var $group = $select.find('optgroup[label="' + catName + '"]');
                items.forEach(function (i) {
                    $group.append('<option value="' + i.id + '">' + escapeHtml(i.name) + '</option>');
                });
            });

            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    // All categories – Restaurant + Rooms + Tours + Laundry + Other
    else if (category === 'all') {
        $.when(
            $.getJSON('/api/menu/getItems'),
            $.getJSON('/api/room/GetRoomCategories'),
            $.getJSON('/api/tourType/getItems'),
            $.getJSON('/api/laundry/getItems'),
            $.getJSON('/api/otherType/getItems')
        ).done(function (menuRes, roomRes, tourRes, laundryRes, otherRes) {
            var groups = {};

            // Menu items (Restaurant) - category-wise
            var menuData = menuRes && menuRes[0] ? menuRes[0] : [];
            var allMenuItems = [];
            menuData.forEach(function (cat) {
                (cat.items || []).forEach(function (itm) {
                    allMenuItems.push({
                        id: itm.id,
                        name: itm.name,
                        price: itm.price,
                        category: cat.category
                    });
                });
            });
            allMenuItems.forEach(function (i) {
                if (!groups[i.category]) groups[i.category] = [];
                groups[i.category].push(i);
            });

            // Rooms
            var roomData = roomRes && roomRes[0] ? roomRes[0] : [];
            if (roomData && roomData.length) {
                var roomGroup = 'Rooms';
                groups[roomGroup] = groups[roomGroup] || [];
                roomData.forEach(function (itm) {
                    groups[roomGroup].push({
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: 0,
                        category: roomGroup
                    });
                });
            }

            // Tours
            var tourData = tourRes && tourRes[0] ? tourRes[0] : [];
            if (tourData && tourData.length) {
                var tourGroup = 'Tours';
                groups[tourGroup] = groups[tourGroup] || [];
                tourData.forEach(function (itm) {
                    groups[tourGroup].push({
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: tourGroup
                    });
                });
            }

            // Laundry
            var laundryData = laundryRes && laundryRes[0] ? laundryRes[0] : [];
            if (laundryData && laundryData.length) {
                var laundryGroup = 'Laundry';
                groups[laundryGroup] = groups[laundryGroup] || [];
                laundryData.forEach(function (itm) {
                    groups[laundryGroup].push({
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: laundryGroup
                    });
                });
            }

            // Other
            var otherData = otherRes && otherRes[0] ? otherRes[0] : [];
            if (otherData && otherData.length) {
                var otherGroup = 'Other';
                groups[otherGroup] = groups[otherGroup] || [];
                otherData.forEach(function (itm) {
                    groups[otherGroup].push({
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: otherGroup
                    });
                });
            }

            // Fill dropdown with optgroups for all groups
            Object.keys(groups).forEach(function (catName) {
                $select.append('<optgroup label="' + escapeHtml(catName) + '"></optgroup>');
                var $group = $select.find('optgroup[label="' + catName + '"]');
                groups[catName].forEach(function (i) {
                    $group.append('<option value="' + i.id + '">' + escapeHtml(i.name) + '</option>');
                });
            });

            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    // Rooms (Stay)
    else if (category === 'rooms') {
        $.getJSON('/api/room/GetRoomCategories', function (data) {
            var groups = { 'Rooms': [] };
            if (data && data.length) {
                data.forEach(function (itm) {
                    var item = {
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: 0,
                        category: 'Rooms'
                    };
                    groups['Rooms'].push(item);
                    $select.append('<option value="' + item.id + '">' + escapeHtml(item.name) + '</option>');
                });
            }
            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    // Tours
    else if (category === 'tours') {
        $.getJSON('/api/tourType/getItems', function (data) {
            var groups = { 'Tours': [] };
            if (data && data.length) {
                data.forEach(function (itm) {
                    var item = {
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: 'Tours'
                    };
                    groups['Tours'].push(item);
                    $select.append('<option value="' + item.id + '">' + escapeHtml(item.name) + '</option>');
                });
            }
            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    // Laundry
    else if (category === 'laundry') {
        $.getJSON('/api/laundry/getItems', function (data) {
            var groups = { 'Laundry': [] };
            if (data && data.length) {
                data.forEach(function (itm) {
                    var item = {
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: 'Laundry'
                    };
                    groups['Laundry'].push(item);
                    $select.append('<option value="' + item.id + '">' + escapeHtml(item.name) + '</option>');
                });
            }
            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    // Other
    else if (category === 'other') {
        $.getJSON('/api/otherType/getItems', function (data) {
            var groups = { 'Other': [] };
            if (data && data.length) {
                data.forEach(function (itm) {
                    var item = {
                        id: itm.id || itm.Id,
                        name: itm.name || itm.Name,
                        price: itm.price || itm.Price || 0,
                        category: 'Other'
                    };
                    groups['Other'].push(item);
                    $select.append('<option value="' + item.id + '">' + escapeHtml(item.name) + '</option>');
                });
            }
            finalize(groups);
        }).fail(function () {
            finalize({});
        });
    }
    else {
        // Fallback – no items
        finalize({});
    }
}

function openReportItemSearchPopup() {
    if (!window.reportItemGroups) {
        // If not loaded yet, load options then open
        loadItemOptions();
    }

    var $container = $('#reportItemContainer');
    $container.empty();

    var searchHtml = ''
        + '<div class="modal-body p-3" style="flex: 0 0 auto;">'
        + '  <div class="mb-3">'
        + '    <div class="input-group input-group-lg">'
        + '      <span class="input-group-text bg-primary text-white">'
        + '        <i class="bi bi-search"></i>'
        + '      </span>'
        + '      <input type="text" id="reportItemSearchInput" class="form-control form-control-lg" '
        + '             placeholder="Search items..." autocomplete="off" />'
        + '      <button type="button" id="reportClearSearchBtn" class="btn btn-outline-secondary d-none">'
        + '        <i class="bi bi-x-circle"></i>'
        + '      </button>'
        + '    </div>'
        + '  </div>'
        + '</div>'
        + '<div class="modal-body p-3" style="flex: 1 1 auto; overflow-y: auto;">'
        + '  <div id="reportItemsGrid" class="row g-3"></div>'
        + '  <div id="reportNoItemsFound" class="text-center text-muted py-5 d-none">'
        + '    <i class="bi bi-inbox fs-1"></i>'
        + '    <p class="mt-3 fs-5">No items found</p>'
        + '  </div>'
        + '</div>';

    $container.html(searchHtml);

    // Build category data array (same structure as invoice popup)
    var categoryData = [];
    if (window.reportItemGroups) {
        Object.keys(window.reportItemGroups).forEach(function (cat) {
            categoryData.push({
                category: cat,
                items: window.reportItemGroups[cat].slice()
            });
        });
    }
    window.reportItemCategoryData = categoryData;

    renderReportItems(categoryData);

    $('#reportItemSearchInput').on('input', function () {
        var term = $(this).val().toLowerCase().trim();
        var $clear = $('#reportClearSearchBtn');
        if (term.length > 0) {
            $clear.removeClass('d-none');
        } else {
            $clear.addClass('d-none');
        }

        var baseData = window.reportItemCategoryData || [];
        // Filter within each category
        var filtered = baseData.map(function (cat) {
            var items = (cat.items || []).filter(function (i) {
                return i.name.toLowerCase().indexOf(term) !== -1
                    || (cat.category && cat.category.toLowerCase().indexOf(term) !== -1);
            });
            return { category: cat.category, items: items };
        });
        renderReportItems(filtered, term.length > 0);
    });

    $('#reportClearSearchBtn').on('click', function () {
        $('#reportItemSearchInput').val('').trigger('input');
    });

    var modalEl = document.getElementById('reportItemModal');
    if (modalEl) {
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        $('#reportItemModal').on('shown.bs.modal', function () {
            $('#reportItemSearchInput').focus();
        });
    }
}

function renderReportItems(categoryData, isSearch) {
    var $grid = $('#reportItemsGrid');
    var $empty = $('#reportNoItemsFound');
    if (!$grid.length) return;

    $grid.empty();

    if (!categoryData || categoryData.length === 0) {
        $empty.removeClass('d-none');
        return;
    }

    // Check if all categories have zero items
    var hasAny = categoryData.some(function (cat) {
        return cat.items && cat.items.length > 0;
    });

    if (!hasAny) {
        $empty.removeClass('d-none');
        return;
    }

    $empty.addClass('d-none');

    // Build tabs like invoice item picker
    var tabButtons = '<ul class="nav nav-tabs nav-tabs-lg mb-3" style="flex-wrap: wrap;">';
    var tabContent = '<div class="tab-content">';

    categoryData.forEach(function (cat, index) {
        var active = index === 0 ? 'active' : '';
        var show = index === 0 ? 'show active' : '';
        var tabId = 'reportTab' + index;

        tabButtons +=
            '<li class="nav-item">' +
            '  <button class="nav-link ' + active + ' px-3 py-2" data-bs-toggle="tab" ' +
            '          data-bs-target="#' + tabId + '" type="button" style="font-size: 1rem; min-height: 48px;">' +
            escapeHtml(cat.category || '') +
            '  </button>' +
            '</li>';

        tabContent +=
            '<div class="tab-pane fade ' + show + '" id="' + tabId + '">' +
            '  <div class="row g-3">';

        if (cat.items && cat.items.length > 0) {
            cat.items.forEach(function (item, i) {
                tabContent += createReportItemCard(item, i);
            });
        } else {
            tabContent +=
                '<div class="col-12">' +
                '  <div class="text-center text-muted py-5">' +
                '    <i class="bi bi-inbox fs-1"></i>' +
                '    <p class="mt-3 fs-5">No items available</p>' +
                '  </div>' +
                '</div>';
        }

        tabContent += '  </div></div>';
    });

    tabButtons += '</ul>';
    tabContent += '</div>';

    $grid.html(tabButtons + tabContent);

    // Click handlers
    $grid.off('click', '.report-item-card');
    $grid.on('click', '.report-item-card', function (e) {
        e.preventDefault();
        var id = $(this).data('item-id');
        if (id) {
            $('#itemId').val(id).trigger('change'); // update select2 as well
        }
        var modalEl = document.getElementById('reportItemModal');
        if (modalEl) {
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();
        }
    });
}

function createReportItemCard(item, index) {
    // Same gradient style as invoice item picker
    var gradients = [
        'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
        'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
        'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
        'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
        'linear-gradient(135deg, #30cfd0 0%, #330867 100%)',
        'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
        'linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%)'
    ];
    var gradientIndex = (item.id || index || 0) % gradients.length;
    var gradient = gradients[gradientIndex];
    var baseCurrency = 'LKR';

    return ''
        + '<div class="col-3 col-lg-8per-row itemCardWrapper">'
        + '  <div class="card shadow-sm h-100 border-0 itemCard report-item-card" '
        + '       data-item-id="' + item.id + '" '
        + '       style="cursor: pointer; transition: all 0.2s ease; background: ' + gradient + ';">'
        + '    <div class="card-body text-center p-2 d-flex flex-column justify-content-center" style="min-height: 80px;">'
        + '      <h6 class="fw-bold text-white mb-1" style="font-size: 0.85rem; line-height: 1.2;">'
        +          escapeHtml(item.name)
        + '      </h6>'
        + '      <div class="text-white fw-bold mt-auto" style="font-size: 0.9rem;">'
        +          baseCurrency + ' ' + (item.price ? Number(item.price).toLocaleString() : '0.00')
        + '      </div>'
        + '    </div>'
        + '  </div>'
        + '</div>';
}
function initItemSelect2() {
    if ($.fn.select2) {
        $('#itemId').select2({
            theme: 'bootstrap-5',
            placeholder: '-- All Items --',
            allowClear: true,
            width: '100%'
        });
    }
}

function loadItemSales() {
    $('#itemSalesLoadingIndicator').show();
    $('#itemSalesTableDesktop').hide();

    var requestData = {
        itemId: parseInt($('#itemId').val()) || 0,
        fromDate: $('#fromDate').val() || null,
        toDate: $('#toDate').val() || null,
        category: $('#categoryFilter').val() || 'All',
        page: itemSalesState.page || 1,
        pageSize: itemSalesState.pageSize || 50
    };

    $.ajax({
        url: '/Reports/GetItemWiseSales',
        type: 'GET',
        data: requestData,
        success: function (response) {
            if (response && response.success) {
                renderItemSales(
                    response.items || [],
                    response.totals || null,
                    response.paging || null
                );
            } else {
                renderItemSales([], null, null);
            }
        },
        error: function () {
            renderItemSales([], null, null);
        },
        complete: function () {
            $('#itemSalesLoadingIndicator').hide();
        }
    });
}

function renderItemSales(items, totals, paging) {
    var html = '';

    if (!items || items.length === 0) {
        html += '<tr><td colspan="4" class="text-center text-muted">No data found.</td></tr>';
        $('#itemSalesTotalQty').text('0.00');
        $('#itemSalesTotalAmount').text('LKR 0.00');
        $('#itemSalesTableBody').html(html);
        $('#itemSalesTableDesktop').show();
        $('#itemSalesPagination').html('');
        return;
    }

    items.forEach(function (row) {
        html += '<tr>';
        html += '<td>' + escapeHtml(row.description) + '</td>';
        html += '<td class="text-end">' + Number(row.quantity || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '</td>';
        html += '<td class="text-end">' + formatCurrency(row.total || 0) + '</td>';
        html += '<td class="text-center">'
            + '<button type="button" class="btn btn-sm btn-outline-primary item-invoices-btn" '
            + 'data-item-id="' + row.itemId + '" '
            + 'data-item-name="' + escapeHtml(row.description) + '">View</button>'
            + '</td>';
        html += '</tr>';
    });

    $('#itemSalesTableBody').html(html);

    if (totals) {
        $('#itemSalesTotalQty').text(Number(totals.quantity || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        $('#itemSalesTotalAmount').text(formatCurrency(totals.total || 0));
    } else {
        $('#itemSalesTotalQty').text('0.00');
        $('#itemSalesTotalAmount').text('LKR 0.00');
    }

    // Render pagination if paging info is available
    renderItemSalesPagination(paging);

    $('#itemSalesTableDesktop').show();
}

function renderItemSalesPagination(paging) {
    var $container = $('#itemSalesPagination');
    if (!paging || !paging.totalPages || paging.totalPages <= 1) {
        $container.html('');
        return;
    }

    var currentPage = paging.page || 1;
    var totalPages = paging.totalPages;

    var html = '<nav aria-label="Item sales pagination"><ul class="pagination pagination-sm justify-content-end mb-0">';

    // Previous
    var prevDisabled = currentPage <= 1 ? ' disabled' : '';
    html += '<li class="page-item' + prevDisabled + '">';
    html += '<a class="page-link" href="#" data-page="' + (currentPage - 1) + '" aria-label="Previous">&laquo;</a>';
    html += '</li>';

    // Page numbers (simple window)
    var start = Math.max(1, currentPage - 2);
    var end = Math.min(totalPages, currentPage + 2);

    for (var i = start; i <= end; i++) {
        var active = i === currentPage ? ' active' : '';
        html += '<li class="page-item' + active + '">';
        html += '<a class="page-link" href="#" data-page="' + i + '">' + i + '</a>';
        html += '</li>';
    }

    // Next
    var nextDisabled = currentPage >= totalPages ? ' disabled' : '';
    html += '<li class="page-item' + nextDisabled + '">';
    html += '<a class="page-link" href="#" data-page="' + (currentPage + 1) + '" aria-label="Next">&raquo;</a>';
    html += '</li>';

    html += '</ul></nav>';

    $container.html(html);
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

// Open invoices popup for a specific item
$(document).on('click', '.item-invoices-btn', function () {
    var itemId = parseInt($(this).data('item-id')) || 0;
    var itemName = $(this).data('item-name') || '';
    if (!itemId) {
        return;
    }

    var fromDate = $('#fromDate').val() || null;
    var toDate = $('#toDate').val() || null;
    var category = $('#categoryFilter').val() || 'All';

    $('#itemInvoicesModalLabel').text('Invoices for ' + itemName);
    var $content = $('#itemInvoicesContent');
    $content.html('<div class="text-center py-4"><div class="spinner-border text-primary" role="status"></div><p class="mt-2">Loading invoices...</p></div>');

    $.ajax({
        url: '/Reports/GetItemInvoices',
        type: 'GET',
        data: {
            itemId: itemId,
            fromDate: fromDate,
            toDate: toDate,
            category: category
        },
        success: function (response) {
            if (!response || !response.success || !response.items || response.items.length === 0) {
                $content.html('<div class="text-center text-muted py-4">No invoices found for this item.</div>');
                return;
            }

            var html = '<div class="table-responsive">';
            html += '<table class="table table-sm table-striped align-middle mb-0">';
            html += '<thead><tr>' +
                '<th>Invoice No</th>' +
                '<th>Date</th>' +
                '<th>Customer</th>' +
                '<th class="text-end">Qty</th>' +
                '<th class="text-end">Amount</th>' +
                '</tr></thead><tbody>';

            response.items.forEach(function (inv) {
                html += '<tr>';
                html += '<td><a href="/Internal/Invoices/Edit/' + inv.invoiceNo + '" target="_blank">' + inv.invoiceNo + '</a></td>';
                html += '<td>' + (inv.date || '') + '</td>';
                html += '<td>' + escapeHtml(inv.customer || '') + '</td>';
                html += '<td class="text-end">' + Number(inv.quantity || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '</td>';
                html += '<td class="text-end">' + formatCurrency(inv.amount || 0) + '</td>';
                html += '</tr>';
            });

            html += '</tbody></table></div>';
            $content.html(html);
        },
        error: function () {
            $content.html('<div class="text-center text-danger py-4">Error loading invoices.</div>');
        }
    });

    var modal = new bootstrap.Modal(document.getElementById('itemInvoicesModal'));
    modal.show();
});

