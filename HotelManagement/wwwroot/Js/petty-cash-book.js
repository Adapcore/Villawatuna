/**
 * Petty Cash Book Create/Edit — dynamic rows, per-currency income totals (no conversion), LKR-only expenses
 */
(function () {
    var currencies = window.pettyCashCurrencies || ['LKR', 'USD', 'GBP', 'EUR'];

    function money(n) {
        var v = parseFloat(n);
        if (isNaN(v)) v = 0;
        return 'Rs. ' + v.toFixed(2);
    }

    function parseAmount($el) {
        var v = parseFloat($el.val());
        return isNaN(v) ? 0 : v;
    }

    function currencyOptionsHtml(selected) {
        selected = (selected || 'LKR').toUpperCase();
        return currencies.map(function (c) {
            var code = (c || '').toUpperCase();
            return '<option value="' + code + '"' + (code === selected ? ' selected' : '') + '>' + code + '</option>';
        }).join('');
    }

    function formatByCurrency(byCurrency) {
        return Object.keys(byCurrency).sort().map(function (c) {
            return c + ': ' + byCurrency[c].toFixed(2);
        }).join(' | ');
    }

    function collectIncomeRows() {
        var rows = [];
        $('#incomeTable tbody tr').each(function (index) {
            var $tr = $(this);
            rows.push({
                id: index + 1,
                title: ($tr.find('.income-title').val() || '').trim(),
                currency: ($tr.find('.income-currency').val() || 'LKR').toUpperCase(),
                amount: parseAmount($tr.find('.income-amount'))
            });
        });
        return rows;
    }

    function collectExpenseRows() {
        var rows = [];
        $('#expenseTable tbody tr').each(function (index) {
            var $tr = $(this);
            rows.push({
                id: index + 1,
                title: ($tr.find('.expense-title').val() || '').trim(),
                currency: 'LKR',
                amount: parseAmount($tr.find('.expense-amount'))
            });
        });
        return rows;
    }

    function recalculateTotals() {
        var income = collectIncomeRows();
        var expenses = collectExpenseRows();

        var byCurrency = {};
        income.forEach(function (r) {
            if (!r.title && r.amount === 0) return;
            var cur = r.currency || 'LKR';
            byCurrency[cur] = (byCurrency[cur] || 0) + r.amount;
        });

        Object.keys(byCurrency).forEach(function (c) {
            byCurrency[c] = Math.round(byCurrency[c] * 100) / 100;
        });

        // LKR income only for balance sheet (no conversion of other currencies)
        var totalIncomeLkr = byCurrency['LKR'] || 0;

        var totalExpensesLkr = 0;
        expenses.forEach(function (r) {
            if (!r.title && r.amount === 0) return;
            totalExpensesLkr += r.amount;
        });
        totalExpensesLkr = Math.round(totalExpensesLkr * 100) / 100;

        var moneyInHand = parseAmount($('#moneyInHand'));
        var handedOver = parseAmount($('#handedOver'));
        var balance = Math.round((moneyInHand + totalIncomeLkr - totalExpensesLkr) * 100) / 100;
        var closeBalance = Math.round((balance - handedOver) * 100) / 100;

        var byCurHtml = formatByCurrency(byCurrency);
        $('#totalIncomeByCurrency').text(byCurHtml || '');
        $('#summaryIncomeByCurrency').text(byCurHtml || '');
        $('#totalExpensesLkrDisplay').text(money(totalExpensesLkr));
        $('#summaryIncome').text(money(totalIncomeLkr));
        $('#summaryExpenses').text(money(totalExpensesLkr));
        $('#summaryBalance').text(money(balance));
        $('#summaryCloseBalance').text(money(closeBalance));

        return Promise.resolve({
            totalIncomeLkr: totalIncomeLkr,
            totalExpensesLkr: totalExpensesLkr,
            balance: balance,
            closeBalance: closeBalance,
            byCurrency: byCurrency
        });
    }

    function addIncomeRow() {
        var html = '<tr>' +
            '<td><input type="text" class="form-control income-title" maxlength="100" placeholder="Title" /></td>' +
            '<td><select class="form-select income-currency">' + currencyOptionsHtml('LKR') + '</select></td>' +
            '<td><input type="number" class="form-control income-amount text-end" step="0.01" min="0" value="0.00" /></td>' +
            '<td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger btn-remove-income" title="Remove"><i class="bi bi-x"></i></button></td>' +
            '</tr>';
        $('#incomeTable tbody').append(html);
    }

    function addExpenseRow() {
        var html = '<tr>' +
            '<td><input type="text" class="form-control expense-title" maxlength="100" placeholder="Title" /></td>' +
            '<td><input type="text" class="form-control expense-currency text-center" value="LKR" readonly tabindex="-1" /></td>' +
            '<td><input type="number" class="form-control expense-amount text-end" step="0.01" min="0" value="0.00" /></td>' +
            '<td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger btn-remove-expense" title="Remove"><i class="bi bi-x"></i></button></td>' +
            '</tr>';
        $('#expenseTable tbody').append(html);
    }

    function loadOpeningBalanceForDate() {
        if ($('#isEdit').val() === 'true') return;

        var date = $('#entryDate').val();
        if (!date) return;

        $.getJSON('/PettyCashBook/GetOpeningBalance', { date: date })
            .done(function (res) {
                if (!res.success) return;
                if (res.exists) {
                    $('#dateWarning').text(res.message + ' Redirecting to edit...').show();
                    setTimeout(function () {
                        window.location.href = '/PettyCashBook/Edit/' + res.id;
                    }, 800);
                    return;
                }
                $('#dateWarning').hide().text('');
                $('#openingBalance').val(parseFloat(res.openingBalance || 0).toFixed(2));
            });
    }

    function saveEntry() {
        var $btn = $('#btnSave');
        $btn.prop('disabled', true);

        recalculateTotals().then(function () {
            var income = collectIncomeRows().filter(function (r) {
                return r.title || r.amount !== 0;
            });
            var expenses = collectExpenseRows().filter(function (r) {
                return r.title || r.amount !== 0;
            });

            var payload = {
                id: parseInt($('#entryId').val(), 10) || 0,
                date: $('#entryDate').val(),
                openingBalance: parseAmount($('#openingBalance')),
                moneyInHand: parseAmount($('#moneyInHand')),
                handedOver: parseAmount($('#handedOver')),
                income: income,
                expences: expenses
            };

            return $.ajax({
                url: '/PettyCashBook/Save',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload)
            });
        }).then(function (response) {
            if (response.success) {
                if (typeof showToast === 'function') {
                    showToast(response.message || 'Saved.', 'success');
                }
                window.location.href = response.redirectUrl || '/PettyCashBook';
            } else {
                if (typeof showToast === 'function') {
                    showToast(response.message || 'Save failed.', 'error');
                }
                $btn.prop('disabled', false);
            }
        }).catch(function (xhr) {
            var msg = (xhr.responseJSON && xhr.responseJSON.message) || xhr.message || 'Save failed.';
            if (typeof showToast === 'function') {
                showToast(msg, 'error');
            }
            $btn.prop('disabled', false);
        });
    }

    $(document).ready(function () {
        $('#btnAddIncome').on('click', function () {
            addIncomeRow();
            recalculateTotals();
        });
        $('#btnAddExpense').on('click', function () {
            addExpenseRow();
            recalculateTotals();
        });

        $(document).on('click', '.btn-remove-income', function () {
            if ($('#incomeTable tbody tr').length <= 1) {
                $(this).closest('tr').find('input').val('');
                $(this).closest('tr').find('select').val('LKR');
                $(this).closest('tr').find('.income-amount').val('0.00');
            } else {
                $(this).closest('tr').remove();
            }
            recalculateTotals();
        });

        $(document).on('click', '.btn-remove-expense', function () {
            if ($('#expenseTable tbody tr').length <= 1) {
                $(this).closest('tr').find('.expense-title').val('');
                $(this).closest('tr').find('.expense-currency').val('LKR');
                $(this).closest('tr').find('.expense-amount').val('0.00');
            } else {
                $(this).closest('tr').remove();
            }
            recalculateTotals();
        });

        $(document).on('input change', '.income-title, .income-currency, .income-amount, .expense-title, .expense-amount, #moneyInHand, #handedOver', function () {
            recalculateTotals();
        });

        $('#entryDate').on('change', loadOpeningBalanceForDate);
        $('#btnSave').on('click', saveEntry);

        recalculateTotals();
    });
})();
