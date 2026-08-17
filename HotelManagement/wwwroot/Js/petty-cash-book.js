/**
 * Petty Cash Book Create/Edit — cash income/expenses (readonly labels)
 */
(function () {
    function formatNumber(n) {
        var v = parseFloat(n);
        if (isNaN(v)) v = 0;
        return v.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function money(n) {
        return 'Rs. ' + formatNumber(n);
    }

    function moneyWithCurrency(currency, n) {
        return escapeHtml(currency || '') + ' ' + formatNumber(n);
    }

    function parseAmount($el) {
        var v = parseFloat($el.val());
        return isNaN(v) ? 0 : v;
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function resolveIncomeTypeCode(code, title) {
        var typeCode = (code || '').trim();
        if (typeCode) return typeCode;

        var parts = String(title || '').split(' - ');
        var last = (parts[parts.length - 1] || '').trim();
        var map = {
            'Dining': 'Dining',
            'Take Away': 'TakeAway',
            'Stay': 'Stay',
            'Other': 'Other',
            'Tour': 'Tour',
            'Laundry': 'Laundry'
        };
        return map[last] || last.replace(/\s+/g, '');
    }

    function incomeTypeClass(typeCode) {
        if (!typeCode) return '';
        return 'income-type-' + String(typeCode).toLowerCase();
    }

    function collectReadonlyRows(tableSelector, emptyClass, includeInvoiceType) {
        var rows = [];
        $(tableSelector + ' tbody tr').each(function (index) {
            var $tr = $(this);
            if ($tr.hasClass(emptyClass)) return;

            var title = ($tr.attr('data-title') || '').trim();
            var currency = ($tr.attr('data-currency') || 'LKR').trim().toUpperCase();
            var amount = parseFloat($tr.attr('data-amount'));
            if (isNaN(amount)) amount = 0;

            var row = {
                id: index + 1,
                title: title,
                currency: currency,
                amount: amount
            };

            if (includeInvoiceType) {
                row.invoiceTypeCode = resolveIncomeTypeCode($tr.attr('data-invoice-type-code'), title);
            }

            rows.push(row);
        });
        return rows;
    }

    function collectIncomeRows() {
        return collectReadonlyRows('#incomeTable', 'income-empty-row', true);
    }

    function collectExpenseRows() {
        return collectReadonlyRows('#expenseTable', 'expense-empty-row');
    }

    function enforceHandedOverRefillExclusive(changedId) {
        var $handed = $('#handedOver');
        var $refill = $('#pettyCashRefillClosing');
        var handed = parseAmount($handed);
        var refill = parseAmount($refill);

        if (changedId === 'handedOver' && handed > 0) {
            $refill.val('0.00');
        } else if (changedId === 'pettyCashRefillClosing' && refill > 0) {
            $handed.val('0.00');
        }
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

        var totalIncomeLkr = byCurrency['LKR'] || 0;

        var otherCurrencyHtml = '';
        Object.keys(byCurrency).sort().forEach(function (c) {
            if (c === 'LKR') return;
            otherCurrencyHtml +=
                '<div class="d-flex justify-content-between flex-wrap gap-1">' +
                '<span>Total Income (' + escapeHtml(c) + ')</span>' +
                '<span>' + moneyWithCurrency(c, byCurrency[c]) + '</span>' +
                '</div>';
        });
        $('#totalIncomeOtherCurrencies').html(otherCurrencyHtml);

        var totalExpensesLkr = 0;
        expenses.forEach(function (r) {
            if (!r.title && r.amount === 0) return;
            totalExpensesLkr += r.amount;
        });
        totalExpensesLkr = Math.round(totalExpensesLkr * 100) / 100;

        var openingBalance = parseAmount($('#openingBalance'));
        var refillOpening = parseAmount($('#pettyCashRefillOpening'));
        var refillClosing = parseAmount($('#pettyCashRefillClosing'));
        var handedOver = parseAmount($('#handedOver'));
        var pettyCashBalance = Math.round((openingBalance + refillOpening) * 100) / 100;
        var balance = Math.round((openingBalance + refillOpening + totalIncomeLkr - totalExpensesLkr) * 100) / 100;
        var closeBalance = Math.round((balance - handedOver + refillClosing) * 100) / 100;

        $('#totalIncomeLkrDisplay').text(money(totalIncomeLkr));
        $('#totalExpensesLkrDisplay').text(money(totalExpensesLkr));
        $('#summaryOpening').text(money(openingBalance));
        $('#summaryPettyCashBalance').text(money(pettyCashBalance));
        $('#summaryIncome').text(money(totalIncomeLkr));
        $('#summaryExpenses').text(money(totalExpensesLkr));
        $('#summaryBalance').text(money(balance));
        $('#summaryCloseBalance').text(money(closeBalance));

        return Promise.resolve({
            totalIncomeLkr: totalIncomeLkr,
            totalExpensesLkr: totalExpensesLkr,
            balance: balance,
            closeBalance: closeBalance,
            refillOpening: refillOpening,
            refillClosing: refillClosing
        });
    }

    function removeLineButtonHtml() {
        return '<td class="text-center petty-cash-line-action">' +
            '<button type="button" class="btn btn-sm btn-outline-danger pcb-remove-line" title="Remove line">' +
            '<i class="bi bi-x-lg"></i>' +
            '</button></td>';
    }

    function renderReadonlyRows(tableSelector, lines, emptyClass, emptyMessage, titleClass, currencyClass, amountClass) {
        var $tbody = $(tableSelector + ' tbody');
        $tbody.empty();

        if (!lines || !lines.length) {
            $tbody.append('<tr class="' + emptyClass + '"><td colspan="4" class="text-center text-muted py-3">' + emptyMessage + '</td></tr>');
            return;
        }

        lines.forEach(function (line, index) {
            var title = line.title || '';
            var currency = (line.currency || 'LKR').toUpperCase();
            var amount = line.amount != null ? Number(line.amount) : 0;
            if (isNaN(amount)) amount = 0;

            var typeCode = '';
            var typeClass = '';
            var typeAttr = '';
            if (titleClass === 'income-title') {
                typeCode = resolveIncomeTypeCode(line.invoiceTypeCode, title);
                typeClass = incomeTypeClass(typeCode);
                typeAttr = ' data-invoice-type-code="' + escapeHtml(typeCode) + '"';
            }

            var html = '<tr' + (typeClass ? ' class="' + typeClass + '"' : '') +
                ' data-line-id="' + (index + 1) + '"' +
                ' data-title="' + escapeHtml(title) + '"' +
                ' data-currency="' + escapeHtml(currency) + '"' +
                ' data-amount="' + amount.toFixed(2) + '"' +
                typeAttr + '>' +
                '<td><span class="' + titleClass + '">' + escapeHtml(title) + '</span></td>' +
                '<td class="text-center"><span class="' + currencyClass + '">' + escapeHtml(currency) + '</span></td>' +
                '<td class="text-end petty-cash-amount-cell" data-currency="' + escapeHtml(currency) + '">' +
                '<span class="' + amountClass + '">' + formatNumber(amount) + '</span></td>' +
                removeLineButtonHtml() +
                '</tr>';
            $tbody.append(html);
        });
    }

    function removeTableLine($btn) {
        var $tr = $btn.closest('tr');
        var $table = $tr.closest('table');
        var $tbody = $table.find('tbody');
        var isIncome = $table.attr('id') === 'incomeTable';
        var emptyClass = isIncome ? 'income-empty-row' : 'expense-empty-row';
        var emptyMessage = isIncome ? 'No cash payments for this date.' : 'No cash expenses for this date.';

        $tr.remove();

        if ($tbody.find('tr:not(.' + emptyClass + ')').length === 0) {
            $tbody.append(
                '<tr class="' + emptyClass + '"><td colspan="4" class="text-center text-muted py-3">' +
                emptyMessage + '</td></tr>'
            );
        }

        recalculateTotals();
    }

    function renderIncomeRows(lines) {
        renderReadonlyRows(
            '#incomeTable',
            lines,
            'income-empty-row',
            'No cash payments for this date.',
            'income-title',
            'income-currency',
            'income-amount'
        );
    }

    function renderExpenseRows(lines) {
        renderReadonlyRows(
            '#expenseTable',
            lines,
            'expense-empty-row',
            'No cash expenses for this date.',
            'expense-title',
            'expense-currency',
            'expense-amount'
        );
    }

    function setOpeningBalance(value) {
        var amount = parseFloat(value);
        if (isNaN(amount)) amount = 0;
        amount = Math.round(amount * 100) / 100;
        $('#openingBalance').val(amount.toFixed(2));
        $('#openingBalanceDisplay').val(formatNumber(amount));
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
                setOpeningBalance(res.openingBalance || 0);
                renderIncomeRows(res.income || []);
                renderExpenseRows(res.expenses || []);
                recalculateTotals();
            });
    }

    function refreshIncomeFromPayments() {
        var date = $('#entryDate').val();
        if (!date) {
            if (typeof showToast === 'function') {
                showToast('Please select a date first.', 'error');
            }
            return;
        }

        var $btn = $('#btnRefreshIncome');
        $btn.prop('disabled', true);

        $.getJSON('/PettyCashBook/GetIncomeFromPayments', { date: date })
            .done(function (res) {
                if (!res.success) {
                    if (typeof showToast === 'function') {
                        showToast(res.message || 'Failed to refresh income.', 'error');
                    }
                    return;
                }
                renderIncomeRows(res.income || []);
                recalculateTotals();
                if (typeof showToast === 'function') {
                    showToast('Income refreshed from cash payments.', 'success');
                }
            })
            .fail(function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) || 'Failed to refresh income.';
                if (typeof showToast === 'function') {
                    showToast(msg, 'error');
                }
            })
            .always(function () {
                $btn.prop('disabled', false);
            });
    }

    function refreshExpensesFromExpenses() {
        var date = $('#entryDate').val();
        if (!date) {
            if (typeof showToast === 'function') {
                showToast('Please select a date first.', 'error');
            }
            return;
        }

        var $btn = $('#btnRefreshExpenses');
        $btn.prop('disabled', true);

        $.getJSON('/PettyCashBook/GetExpensesFromExpenses', { date: date })
            .done(function (res) {
                if (!res.success) {
                    if (typeof showToast === 'function') {
                        showToast(res.message || 'Failed to refresh expenses.', 'error');
                    }
                    return;
                }
                renderExpenseRows(res.expenses || []);
                recalculateTotals();
                if (typeof showToast === 'function') {
                    showToast('Expenses refreshed from cash expenses.', 'success');
                }
            })
            .fail(function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) || 'Failed to refresh expenses.';
                if (typeof showToast === 'function') {
                    showToast(msg, 'error');
                }
            })
            .always(function () {
                $btn.prop('disabled', false);
            });
    }

    function saveEntry() {
        var $btn = $('#btnSave');
        var date = $('#entryDate').val();
        if (!date) {
            if (typeof showToast === 'function') {
                showToast('Please select a date first.', 'error');
            }
            $('#entryDate').focus();
            return;
        }

        $btn.prop('disabled', true);

        enforceHandedOverRefillExclusive('handedOver');
        enforceHandedOverRefillExclusive('pettyCashRefillClosing');

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
                pettyCashRefillOpening: parseAmount($('#pettyCashRefillOpening')),
                pettyCashRefillClosing: parseAmount($('#pettyCashRefillClosing')),
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
        $(document).on('input change', '#pettyCashRefillOpening', function () {
            recalculateTotals();
        });

        $(document).on('input change', '#handedOver, #pettyCashRefillClosing', function () {
            enforceHandedOverRefillExclusive(this.id);
            recalculateTotals();
        });

        $('#entryDate').on('change', loadOpeningBalanceForDate);
        $('#btnRefreshIncome').on('click', refreshIncomeFromPayments);
        $('#btnRefreshExpenses').on('click', refreshExpensesFromExpenses);
        $('#btnSave').on('click', saveEntry);

        $(document).on('click', '#incomeTable .pcb-remove-line, #expenseTable .pcb-remove-line', function (e) {
            e.preventDefault();
            removeTableLine($(this));
        });

        recalculateTotals();
    });
})();
