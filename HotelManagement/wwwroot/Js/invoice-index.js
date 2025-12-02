/**
 * Invoice Index Page JavaScript
 * Handles delete functionality for invoice list page
 */

$(document).ready(function() {
    // Delete invoice handler - use event delegation and prevent navigation
    $(document).on('click', '.btn-delete-invoice', function(e) {
        // Prevent navigation when clicking delete button inside a link
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        
        var invoiceId = $(this).data('invoice-id');
        var invoiceNo = $(this).data('invoice-no');
        
        // Show confirmation dialog using common component
        showConfirmDialog(
            'Are you sure you want to delete Invoice #' + invoiceNo + '?',
            function(confirmed) {
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
        
        return false; // Additional prevention
    });

    // View payments handler (for both desktop and mobile)
    $(document).on('click', '.btn-view-payments, .btn-view-payments-icon', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        // Check if button is disabled
        if ($(this).hasClass('disabled') || $(this).prop('disabled')) {
            return;
        }
        
        var invoiceNo = $(this).data('invoice-no');
        if (!invoiceNo) {
            console.error('Invoice number not found');
            return;
        }

        // Update modal title
        $('#modalInvoiceNo').text(invoiceNo);
        
        // Show loading state
        $('#invoicePaymentsModalBody').html(
            '<div class="text-center py-5">' +
            '<div class="spinner-border text-primary" role="status">' +
            '<span class="visually-hidden">Loading...</span>' +
            '</div>' +
            '<p class="mt-3">Loading payments...</p>' +
            '</div>'
        );

        // Show modal
        var modalElement = document.getElementById('invoicePaymentsModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            var modal = new bootstrap.Modal(modalElement);
            modal.show();
        } else {
            $('#invoicePaymentsModal').modal('show');
        }

        // Fetch payments
        $.getJSON('/Internal/Invoices/GetPayments/' + invoiceNo, function(response) {
            if (response && response.success && response.data) {
                renderPayments(response.data);
            } else {
                $('#invoicePaymentsModalBody').html(
                    '<div class="alert alert-info">No payments found for this invoice.</div>'
                );
            }
        }).fail(function(xhr, status, error) {
            console.error('Error loading payments:', error);
            $('#invoicePaymentsModalBody').html(
                '<div class="alert alert-danger">Error loading payments. Please try again.</div>'
            );
        });
    });
});

/**
 * Delete invoice via AJAX
 * @param {number} invoiceId - The ID of the invoice to delete
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
        success: function(response) {
            if (response.success) {
                // Show success message
                if (typeof showToastSuccess === 'function') {
                    showToastSuccess(response.message || 'Invoice deleted successfully.');
                } else {
                    alert(response.message || 'Invoice deleted successfully.');
                }
                
                // Reload the page after a short delay
                setTimeout(function() {
                    window.location.reload();
                }, 1000);
            } else {
                // Show error message
                if (typeof showToastError === 'function') {
                    showToastError(response.message || 'Error deleting invoice.');
                } else {
                    alert(response.message || 'Error deleting invoice.');
                }
            }
        },
        error: function(xhr, status, error) {
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
 * @param {Array} payments - Array of payment objects
 */
function renderPayments(payments) {
    if (!payments || payments.length === 0) {
        $('#invoicePaymentsModalBody').html(
            '<div class="alert alert-info">No payments found for this invoice.</div>'
        );
        return;
    }

    // Calculate total
    var totalAmount = payments.reduce(function(sum, payment) {
        return sum + parseFloat(payment.amount || 0);
    }, 0);

    // Group payments by date
    var groupedPayments = {};
    payments.forEach(function(payment) {
        var dateKey = payment.date;
        if (!groupedPayments[dateKey]) {
            groupedPayments[dateKey] = [];
        }
        groupedPayments[dateKey].push(payment);
    });

    // Sort dates descending
    var sortedDates = Object.keys(groupedPayments).sort(function(a, b) {
        return new Date(b) - new Date(a);
    });

    var html = '<div class="payments-container">';
    
    sortedDates.forEach(function(date) {
        var datePayments = groupedPayments[date];
        var dateTotal = datePayments.reduce(function(sum, p) {
            return sum + parseFloat(p.amount || 0);
        }, 0);
        
        html += '<div class="payment-date-group mb-3">';
        html += '<div class="payment-date-header bg-light p-2 rounded">';
        html += '<h6 class="mb-0 fw-bold">' + formatDate(date) + '</h6>';
        html += '<small class="text-muted">Total: ' + formatCurrency(dateTotal) + '</small>';
        html += '</div>';
        
        html += '<div class="payment-list">';
        datePayments.forEach(function(payment) {
            html += '<div class="payment-row card mb-2">';
            html += '<div class="card-body p-3">';
            html += '<div class="row align-items-center">';
            html += '<div class="col-6">';
            html += '<div class="fw-bold">' + formatCurrency(payment.amount) + '</div>';
            html += '<small class="text-muted">' + getPaymentTypeBadge(payment.type) + '</small>';
            html += '</div>';
            html += '<div class="col-6 text-end">';
            if (payment.reference) {
                html += '<small class="text-muted d-block">Ref: ' + escapeHtml(payment.reference) + '</small>';
            }
            html += '<small class="text-muted">ID: #' + payment.id + '</small>';
            html += '</div>';
            html += '</div>';
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
 * Format date for display
 */
function formatDate(dateString) {
    var date = new Date(dateString);
    var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[date.getMonth()] + ' ' + date.getDate() + ', ' + date.getFullYear();
}

/**
 * Format currency for display
 */
function formatCurrency(value) {
    var num = Number(value || 0);
    return 'LKR ' + num.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/**
 * Get payment type badge HTML
 */
function getPaymentTypeBadge(type) {
    var badgeClass = 'bg-secondary';
    var icon = 'bi-credit-card';
    
    switch(type.toLowerCase()) {
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

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    var map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return (text || '').replace(/[&<>"']/g, function(m) { return map[m]; });
}

