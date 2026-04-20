// Shared confirm dialogs for leave actions (Approve/Reject)
(function () {
  function bindConfirmButtons(selector, messageTemplate, optionsBuilder) {
    var buttons = document.querySelectorAll(selector);
    if (!buttons || !buttons.length) return;

    buttons.forEach(function (btn) {
      btn.addEventListener('click', function (e) {
        if (typeof showConfirmDialog !== 'function') return;

        e.preventDefault();
        e.stopPropagation();

        var form = btn.closest('form');
        if (!form) return;

        var leaveId = btn.getAttribute('data-leave-id') || '';
        var msg = messageTemplate.replace('{id}', leaveId);
        showConfirmDialog(
          msg,
          function (confirmed) {
            if (confirmed) form.submit();
          },
          optionsBuilder(leaveId)
        );
        return false;
      });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    bindConfirmButtons(
      '.js-leave-approve-btn',
      'Do you want to approve Leave #{id}?',
      function (id) {
        return {
          type: 'Success',
          title: 'Approve Leave',
          yesButtonText: 'Yes, Approve',
          noButtonText: 'No',
          dialogId: 'approveLeaveDialog_' + id,
        };
      }
    );

    bindConfirmButtons(
      '.js-leave-reject-btn',
      'Do you want to reject Leave #{id}?',
      function (id) {
        return {
          type: 'Danger',
          title: 'Reject Leave',
          yesButtonText: 'Yes, Reject',
          noButtonText: 'No',
          warningText: 'This action cannot be undone.',
          dialogId: 'rejectLeaveDialog_' + id,
        };
      }
    );
  });
})();

