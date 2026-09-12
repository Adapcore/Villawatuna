/**
 * Petty Cash Book Details — Approve / Reopen (admin)
 */
(function () {
    function getToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    function postStatusAction(url, id, successFallbackMessage) {
        return $.ajax({
            url: url,
            type: 'POST',
            data: { id: id, __RequestVerificationToken: getToken() }
        }).done(function (response) {
            if (response && response.success) {
                if (typeof showToast === 'function') {
                    showToast(response.message || successFallbackMessage, 'success');
                }
                if (response.redirectUrl) {
                    window.location.href = response.redirectUrl;
                } else {
                    window.location.reload();
                }
            } else if (typeof showToast === 'function') {
                showToast((response && response.message) || 'Action failed.', 'error');
            }
        }).fail(function (xhr) {
            var msg = (xhr.responseJSON && xhr.responseJSON.message) || 'Action failed.';
            if (typeof showToast === 'function') {
                showToast(msg, 'error');
            }
        });
    }

    $(document).ready(function () {
        $('#btnApprovePettyCash').on('click', function () {
            var id = $(this).data('id');
            var run = function () {
                postStatusAction('/PettyCashBook/Approve', id, 'Approved.');
            };

            if (typeof showConfirmDialog === 'function') {
                showConfirmDialog(
                    'Approve this daily cash summary? It will become read-only.',
                    function (confirmed) {
                        if (confirmed) run();
                    },
                    {
                        type: 'Success',
                        title: 'Approve Entry',
                        yesButtonText: 'Yes, Approve',
                        noButtonText: 'No',
                        dialogId: 'approvePettyCashDialog'
                    }
                );
            } else if (window.confirm('Approve this daily cash summary?')) {
                run();
            }
        });

        $('#btnReopenPettyCash').on('click', function () {
            var id = $(this).data('id');
            var run = function () {
                postStatusAction('/PettyCashBook/Reopen', id, 'Reopened.');
            };

            if (typeof showConfirmDialog === 'function') {
                showConfirmDialog(
                    'Reopen this daily cash summary so it can be edited again?',
                    function (confirmed) {
                        if (confirmed) run();
                    },
                    {
                        type: 'Warning',
                        title: 'Reopen Entry',
                        yesButtonText: 'Yes, Reopen',
                        noButtonText: 'No',
                        dialogId: 'reopenPettyCashDialog'
                    }
                );
            } else if (window.confirm('Reopen this daily cash summary?')) {
                run();
            }
        });
    });
})();
