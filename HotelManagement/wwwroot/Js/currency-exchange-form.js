$(document).ready(function () {
    var $currency = $('#ForeignCurrency');

    function applyDefaultExchangeRate() {
        var selectedCode = (($currency.val() || '') + '').trim();
        if (!selectedCode || !Array.isArray(currencyData)) {
            calculateLkrAmount();
            return;
        }

        var currency = currencyData.find(function (c) {
            var code = ((c.code || c.Code || '') + '').trim();
            return code.toLowerCase() === selectedCode.toLowerCase();
        });

        var rate = currency ? parseFloat(currency.exchangeRate ?? currency.ExchangeRate) : NaN;
        if (!isNaN(rate) && rate > 0) {
            $('#ExchangeRate').val(rate);
        }

        calculateLkrAmount();
    }

    $currency.on('change', applyDefaultExchangeRate);

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

    // Load rate when editing (currency already selected) or after a selection.
    if ($currency.val()) {
        applyDefaultExchangeRate();
    } else {
        calculateLkrAmount();
    }
});

function calculateLkrAmount() {
    var rate = parseFloat($('#ExchangeRate').val()) || 0;
    var amount = parseFloat($('#ForeignAmount').val()) || 0;
    var lkr = Math.round(rate * amount * 100) / 100;
    $('#LKRAmount').val(lkr.toFixed(2));
}
