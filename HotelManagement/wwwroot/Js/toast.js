function showToastSuccess(message) {
    const toastEl = document.getElementById('toast-success');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = message;
    
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
    toast.show();
    
    applyShowToastStyles();
}

function showToastError(message) {
    const toastEl = document.getElementById('toast-error');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = message;
    
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
    toast.show();
    
    applyShowToastStyles();
}

function showToastWarning(message) {
    const toastEl = document.getElementById('toast-warning');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = message;
    
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
    toast.show();
    
    applyShowToastStyles();
}

function closeToastSuccess() {
    const toastEl = document.getElementById('toast-success');
    const toast = bootstrap.Toast.getInstance(toastEl);
    if (toast) {
        toast.hide();
    }
}

function closeToastError() {
    const toastEl = document.getElementById('toast-error');
    const toast = bootstrap.Toast.getInstance(toastEl);
    if (toast) {
        toast.hide();
    }
}

function closeToastWarning() {
    const toastEl = document.getElementById('toast-warning');
    const toast = bootstrap.Toast.getInstance(toastEl);
    if (toast) {
        toast.hide();
    }
}

var myTimeout = null;
function applyShowToastStyles() {
    const toastPanel = document.getElementById('toastPanel');
    if (toastPanel) {
        toastPanel.classList.remove('z-n1');
        toastPanel.classList.add('z-5');
    }

    if (myTimeout != null) {
        clearTimeout(myTimeout);
    }
    myTimeout = setTimeout(applyHideToastStyles, 5000);
}

function applyHideToastStyles() {
    const toastPanel = document.getElementById('toastPanel');
    if (toastPanel) {
        toastPanel.classList.remove('z-5');
        toastPanel.classList.add('z-n1');
    }
}