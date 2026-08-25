$(document).ready(function () {
    $('#ForeignCurrencyId').on('change', function () {
        var selectedId = parseInt($(this).val(), 10);
        if (!selectedId || !Array.isArray(currencyData)) {
            calculateLkrAmount();
            return;
        }

        var currency = currencyData.find(function (c) {
            return c.id === selectedId;
        });

        if (currency && currency.exchangeRate > 0) {
            $('#ExchangeRate').val(currency.exchangeRate);
        }

        calculateLkrAmount();
    });

    $('#ExchangeRate, #ForeignAmount').on('input change', calculateLkrAmount);

    $('#btnSaveExchange').closest('form').on('submit', function () {
        calculateLkrAmount();
        var $submitBtn = $('#btnSaveExchange');
        $submitBtn.prop('disabled', true);
        if (!this.checkValidity()) {
            $submitBtn.prop('disabled', false);
            return true;
        }
        $submitBtn.html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...');
    });

    calculateLkrAmount();
});

function calculateLkrAmount() {
    var rate = parseFloat($('#ExchangeRate').val()) || 0;
    var amount = parseFloat($('#ForeignAmount').val()) || 0;
    var lkr = Math.round(rate * amount * 100) / 100;
    $('#LKRAmount').val(lkr.toFixed(2));
}
