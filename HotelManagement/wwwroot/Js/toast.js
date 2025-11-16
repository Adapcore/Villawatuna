function showToastSuccess(message) {
    $('#toast-success .toast-body').html(message);
    $('#toast-success').toast('show');
    applyShowToastStyles();
}
function showToastError(message) {
    $('#toast-error .toast-body').html(message);
    $('#toast-error').toast('show');
    applyShowToastStyles();
}
function showToastWarning(message) {
    $('#toast-warning .toast-body').html(message);
    $('#toast-warning').toast('show');
    applyShowToastStyles();
}

function closeToastSuccess() {
    $('#toast-success .toast-body').html('');
    $('#toast-success').toast('hide');
}
function closeToastError() {
    $('#toast-error .toast-body').html('');
    $('#toast-error').toast('hide');
}
function closeToastWarning() {
    $('#toast-warning .toast-body').html('');
    $('#toast-warning').toast('hide');
}

var myTimeout = null;
function applyShowToastStyles() {
    $('#toastPanel').removeClass('z-n1');
    $('#toastPanel').addClass('z-5');

    if (myTimeout != null) {
        
        clearTimeout(myTimeout);
    }
    myTimeout = setTimeout(applyHideToastStyles, 5000);
}

function applyHideToastStyles() {
    $('#toastPanel').removeClass('z-5');
    $('#toastPanel').addClass('z-n1');
}