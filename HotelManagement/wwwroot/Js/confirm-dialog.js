/**
 * Common Confirmation Dialog Component
 * Reusable confirmation dialog that can be used across the application
 * 
 * @param {string} message - The message to display in the dialog body
 * @param {function} callback - Callback function that receives true/false based on user's choice
 * @param {object} options - Optional configuration object
 * @param {string} options.type - Dialog type: "Confirm", "Information", "Warning", "Danger", "Success" (default: "Confirm")
 * @param {string} options.title - Dialog title (default: based on type)
 * @param {string} options.yesButtonText - Text for Yes button (default: "Yes")
 * @param {string} options.noButtonText - Text for No button (default: "No")
 * @param {string} options.warningText - Additional warning text to display (default: null)
 * @param {string} options.dialogId - Unique ID for the dialog (default: "commonConfirmDialog")
 */
function showConfirmDialog(message, callback, options) {
    // Default options
    options = options || {};
    var type = options.type || 'Confirm';
    var dialogId = options.dialogId || 'commonConfirmDialog';
    
    // Define color classes based on type
    var typeStyles = {
        'Confirm': {
            headerClass: '',
            titleClass: '',
            yesButtonClass: 'btn-primary',
            title: 'Confirm',
            warningClass: 'text-primary'
        },
        'Information': {
            headerClass: 'bg-info text-white',
            titleClass: 'bg-info text-white',
            yesButtonClass: 'btn-info',
            title: 'Information',
            warningClass: 'text-info'
        },
        'Warning': {
            headerClass: 'bg-warning text-dark',
            titleClass: 'bg-warning text-dark',
            yesButtonClass: 'btn-warning',
            title: 'Warning',
            warningClass: 'text-warning'
        },
        'Danger': {
            headerClass: 'bg-danger text-white',
            titleClass: 'bg-danger text-white',
            yesButtonClass: 'btn-danger',
            title: 'Delete',
            warningClass: 'text-danger'
        },
        'Success': {
            headerClass: 'bg-success text-white',
            titleClass: 'bg-success text-white',
            yesButtonClass: 'btn-success',
            title: 'Success',
            warningClass: 'text-success'
        }
    };
    
    // Get styles for the selected type (default to Confirm if type not found)
    var styles = typeStyles[type] || typeStyles['Confirm'];
    
    // Override with options if provided
    var title = options.title || styles.title;
    var yesButtonText = options.yesButtonText || 'Yes';
    var noButtonText = options.noButtonText || 'No';
    var yesButtonClass = styles.yesButtonClass;
    var headerClass = styles.headerClass;
    var titleClass = styles.titleClass || '';
    var warningText = options.warningText || null;
    var warningClass = styles.warningClass;
    
    // Remove existing dialog if any
    $('#' + dialogId).remove();
    
    // Build header class attribute - ensure entire header section has colored background
    var headerClassCombined = headerClass ? 'modal-header ' + headerClass : 'modal-header';
    var headerClassAttr = ' class="' + headerClassCombined + '"';
    
    // Add inline styles for warning type to ensure yellow background is visible
    var headerStyle = '';
    if (type === 'Warning') {
        headerStyle = ' style="background-color: #ffc107 !important; color: #212529 !important;"';
    } else if (type === 'Danger') {
        headerStyle = ' style="background-color: #dc3545 !important; color: #ffffff !important;"';
    } else if (type === 'Information') {
        headerStyle = ' style="background-color: #0dcaf0 !important; color: #ffffff !important;"';
    } else if (type === 'Success') {
        headerStyle = ' style="background-color: #198754 !important; color: #ffffff !important;"';
    }
    
    var closeButtonClass = headerClass.includes('text-white') || headerClass.includes('bg-danger') || headerClass.includes('bg-success') || headerClass.includes('bg-info')
        ? 'btn-close btn-close-white' 
        : 'btn-close';
    
    // Build warning text HTML if provided
    var warningHtml = warningText ? '<p class="' + warningClass + '"><strong>' + warningText + '</strong></p>' : '';
    
    // Create dialog HTML
    const dialogHtml = `
        <div id="${dialogId}" class="modal fade" tabindex="-1" role="dialog" style="display: block;">
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content">
                    <div${headerClassAttr}${headerStyle}>
                        <h5 class="modal-title${titleClass ? ' ' + titleClass : ''}">${title}</h5>
                        <button type="button" class="${closeButtonClass}" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p>${message}</p>
                        ${warningHtml}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" id="${dialogId}No">${noButtonText}</button>
                        <button type="button" class="btn ${yesButtonClass}" id="${dialogId}Yes">${yesButtonText}</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Append to body
    $('body').append(dialogHtml);
    
    // Show modal using Bootstrap
    const modalElement = document.getElementById(dialogId);
    const modal = new bootstrap.Modal(modalElement, {
        backdrop: 'static',
        keyboard: false
    });
    modal.show();
    
    // Handle Yes button
    $('#' + dialogId + 'Yes').on('click', function() {
        modal.hide();
        $('#' + dialogId).remove();
        if (callback) callback(true);
    });
    
    // Handle No button
    $('#' + dialogId + 'No').on('click', function() {
        modal.hide();
        $('#' + dialogId).remove();
        if (callback) callback(false);
    });
    
    // Handle close button
    $('#' + dialogId + ' .btn-close').on('click', function() {
        modal.hide();
        $('#' + dialogId).remove();
        if (callback) callback(false);
    });
    
    // Handle backdrop click - prevent closing
    $('#' + dialogId).on('click', function(e) {
        if ($(e.target).hasClass('modal')) {
            e.stopPropagation();
        }
    });
}
