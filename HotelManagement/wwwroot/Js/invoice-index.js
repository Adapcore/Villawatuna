/**
 * Invoice Index Page JavaScript
 * Handles delete functionality for invoice list page
 */

$(document).ready(function() {
    // Delete invoice handler
    $(document).on('click', '.btn-delete-invoice', function() {
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

