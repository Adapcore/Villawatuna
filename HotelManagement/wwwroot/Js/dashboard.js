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
            if (res.isAdmin && $('#tileTotalRevenue').length) {
                $('#tileTotalRevenue').text(formatCurrency(res.data.totalRevenue));
            }
            $('#tileExpenses').text(formatCurrency(res.data.totalExpenses));
            $('#tileRestaurantRevenue').text(formatCurrency(res.data.restaurantRevenue));
            $('#tileServiceCharges').text(formatCurrency(res.data.serviceCharges));
            $('#tileLaundryRevenue').text(formatCurrency(res.data.laundryRevenue));
            $('#tileTourRevenue').text(formatCurrency(res.data.tourRevenue));
            $('#tileStayRevenue').text(formatCurrency(res.data.stayRevenue));
        }
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
    });
}

