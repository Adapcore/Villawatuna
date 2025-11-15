(function ($) {
    $.fn.invoice = function (options) {
        return new $.invoice(this, options);
    };

    $.invoice = function (el, options) {
        var defaults = {
            mode: 'Insert',
            createUrl: "/Invoices/Create"
        };

        this.options = $.extend(defaults, options);
        this._mode = this.options.mode;
        this._invoice = this.options.invoice;
        this._type = this.options.invoice.type;
        this.$container = $(el);
        this.itemIndex = 0;
        this.serviceCharge = 0;
        this._baseCurrency = 'LKR';

        this.Init();
        return this;
    };

    $.invoice.fn = $.invoice.prototype = { version: '1.0.0' };
    $.invoice.fn.extend = $.invoice.extend = $.extend;

    $.invoice.fn.extend({
        Init: function () {
            var self = this;

            this.BindEvents();
            this.LoadInvoice();
            this.LoadItems();
            this.BindItemSelection();
            this.LoadServiceCharge();

            // Currency rates will be fetched from API
            //this._rates = [
            //    { 'from': 'LKR', 'to': 'USD', 'rate': 0.00300 },
            //    { 'from': 'USD', 'to': 'LKR', 'rate': 300 },

            //    { 'from': 'LKR', 'to': 'GBP', 'rate': 0.0026 },
            //    { 'from': 'GBP', 'to': 'LKR', 'rate': 380 },

            //    { 'from': 'LKR', 'to': 'EUR', 'rate': 0.0030 },
            //    { 'from': 'EUR', 'to': 'LKR', 'rate': 330 },

            //    { 'from': 'USD', 'to': 'GBP', 'rate': 0.78 },
            //    { 'from': 'GBP', 'to': 'USD', 'rate': 1.28 },
            //]
        },

        LoadInvoice: function () {
            var self = this;
            $("#InvoiceNo").val(self._invoice.invoiceNo);
            $("#Status").val(self._invoice.status);
            $('#txtPaid').html(self._invoice.paid.toFixed(2));
            $('#txtBalance').html(self._invoice.balance.toFixed(2));
            $('#txtChange').html(self._invoice.change.toFixed(2));
            $('#txtLastPaid').html(self._invoice.cash.toFixed(2));

            $("#txtPayment").val(0);
            $("#txtCash").val(0);
            $("#txtBalanceDue").html('');

            if (self._invoice.type == 1 || self._invoice.type == 2) {
                $("#btn_print").attr("href", "/Internal/Invoices/PrintThermal/" + self._invoice.invoiceNo);
            }
            else {
                $("#btn_print").attr("href", "/Internal/Invoices/Print/" + self._invoice.invoiceNo);
            }

            // Load currency rate if available
            if (self._invoice.currencyRate) {
                $("#txtCurrencyRate").val(self._invoice.currencyRate);
            } else {
                self.LoadCurrencyRate();
            }

            $("#PaymentType").val(1);
            $("#txtPaymentReference").val('');
            $("#dv_paymentRef").hide();


            $('#dv_paidWrapper').hide();
            $('#dv_lastPaid').addClass('d-none');
            $('#dv_paymentWrapper').hide();

            $('#btnComplete').hide();
            $('#btnPay').hide();
            $('#btn_print').hide();

            self.DisableHeader();
            self.DisableDetails();

            if (self._invoice.status == 1) { // In-Progress
                self.EnableHeader(0);

                $('#btnComplete').show();
                $('#btnSave').show();

                self.EnableDetails();
            }
            else if (self._invoice.status == 2) { // Complete
                self.EnableHeader(1);

                $('#btnPay').show();
                $('#btn_print').show();
            }
            else if (self._invoice.status == 3) { // Partally Paid
                self.EnableHeader(1);
                $('#dv_paidWrapper').show();
                $('#dv_paymentWrapper').slideDown();
                $('#btn_print').show();
            }
            else if (self._invoice.status == 4) { // Paid
                $('#dv_paidWrapper').show();
                $('#dv_lastPaid').removeClass('d-none');
                $('#btn_print').show();
            }
        },
        BindEvents: function () {
            var self = this;

            // Item Add
            $('#addItemBtn').on("click", () => {
                self.AddItemRow();
            });

            // Item Remove
            $('.removeItemBtn').on("click", (e) => {
                var row = $(e.currentTarget).closest("tr");
                self.RemoveItemRow(row);
            });

            // Quantity change
            $("#invoiceItems tbody").on("change", ".orderQty", function (e) {
                var row = $(e.currentTarget).closest("tr");
                self.UpdateRowTotal(row);
            });

            $("#btnComplete").on("click", function () {
                $("#Status").val(2);
                self.Save();
            });

            $("#btnPay").on("click", function () {
                self.EnablePayment();
            });

            $("#btnSave").on("click", function () {
                self.Save();
            });

            $("#ddlCurrency").on("change", function () {
                self.LoadCurrencyRate();
                self.CalculateTotals();
            });

            $("#txtCurrencyRate").on("change", function () {
                self.CalculateTotals();
            });

            $("#txtCash").on("change", function () {

                if ($("#txtCash").val() === '')
                    $("#txtCash").val(0);

                self.CalculateBalanceDue();
            });

            $("#PaymentType").on("change", function () {
                var val = parseInt($(this).val());
                // Card (3) or Bank Transfer (2) require reference
                if (val === 2 || val === 3) {
                    $("#dv_paymentRef").show();
                } else {
                    $("#dv_paymentRef").hide();
                    $("#txtPaymentReference").val("");
                }
            });
        },
        DisableHeader: function () {
            $("#Date").prop('disabled', true);
            $("#Status").prop('disabled', true);
            $("#CustomerId").prop('disabled', true);
            $("#ddlCurrency").prop('disabled', true);
            $("#txtCurrencyRate").prop('disabled', true);
            $("#ReferenceNo").prop('disabled', true);
            $("#Note").prop('disabled', true);
        },
        EnableHeader: function (level) {
            if (level == 0) {
                $("#Date").prop('disabled', false);
                //$("#Status").prop('disabled', false);
                $("#CustomerId").prop('disabled', false);
                $("#ddlCurrency").prop('disabled', false);
                $("#txtCurrencyRate").prop('disabled', false);
            }
            $("#ReferenceNo").prop('disabled', false);
            $("#Note").prop('disabled', false);
        },
        DisableDetails: function () {
            $("#invoiceItems tbody .form-control").prop('disabled', true);
            $("#invoiceItems tbody .form-select").prop('disabled', true);
            $("#invoiceItems tbody .btn").prop('disabled', true);
            $('#addItemBtn').prop('disabled', true);
        },
        EnableDetails: function () {
            $("#invoiceItems tbody .form-control").prop('disabled', false);
            $("#invoiceItems tbody .form-select").prop('disabled', false);
            $("#invoiceItems tbody .btn").prop('disabled', false);
            $('#addItemBtn').prop('disabled', false);
        },
        EnablePayment: function () {

            $('#btnPay').fadeOut();
            //$('#dv_balance').removeClass('d-none');
            //$('#dv_cash').show();
            //$('#dv_payment').show();
            //$('#dv_balanceDue').removeClass('d-none');
            $('#dv_paymentWrapper').slideDown();
            $('#btn_print').show();
        },
        LoadServiceCharge: function () {
            var self = this;
            $.getJSON("/api/menu/GetServiceCharge", function (data) {
                self.serviceCharge = data;
            });
        },

        LoadCurrencyRate: function () {
            var self = this;
            var fromCurrency = $("#ddlCurrency").val();
            var toCurrency = self._baseCurrency;

            if (fromCurrency && fromCurrency !== toCurrency) {
                $.getJSON("/api/currency/getCurrencyRate", { from: fromCurrency, to: toCurrency })
                    .done(function (data) {
                        $("#txtCurrencyRate").val(data.rate);
                        self.CalculateTotals();
                    })
                    .fail(function () {
                        $("#txtCurrencyRate").val("");
                        console.warn("Failed to load currency rate");
                    });
            } else {
                $("#txtCurrencyRate").val("1");
                self.CalculateTotals();
            }
        },
        LoadItems: function () {
            var self = this;

            if (self._type == 1 || self._type == 2)// Dining, Take away
            {
                $.getJSON("/api/menu/getItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                });
            }
            else if (self._type == 3)// Stay
            {
                $.getJSON("/api/room/GetRoomCategories", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="0">${i.name}</option>`).join('');
                    self.SelectDropDownValue()
                });
            }
            else if (self._type == 4)// Other Types
            {
                $.getJSON("/api/otherType/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue()
                });
            }
            else if (self._type == 6)// Laundry 
            {
                $.getJSON("/api/laundry/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue()
                });
            }
            else if (self._type == 5)// Tour Types
            {
                $.getJSON("/api/tourType/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue()
                });
            }
        },
        SelectDropDownValue: function () {
            var self = this;

            // Populate all selects in the table
            $("#invoiceItems tbody tr").each(function () {
                var $row = $(this);
                var $select = $row.find(".orderItemSelect");
                var selectedId = $row.find(".itemId").val(); // <-- pick from hidden field

                $select.html('<option value="">-- Select --</option>' + self.itemOptions);

                if (selectedId) {
                    $select.val(selectedId); // set dropdown
                    //var selected = $select.find("option:selected");

                    //// update description & price
                    ////var price = parseFloat(selected.data("price")) || 0;
                    //var name = selected.text();

                    //$row.find(".description").val(name);

                }
            });
        },
        AddItemRow: function (item) {
            var self = this;

            // item is an optional object: { Id, Description, UnitPrice } 
            var rowIndex = $("#invoiceItems tbody tr").length;

            var itemId = item?.Id || 0;
            var description = item?.Description || '';
            var unitPrice = item?.UnitPrice || 0.00;

            var rowHtml = `
                        <tr>
                            <td>
                                <input type="hidden" value="@detail.ItemId" class="itemId" />
                                <select class="form-select orderItemSelect">
                                    <option value="">-- Select --</option>
                                    ${this.itemOptions || ""}
                                </select>
                            </td>`;

            if (self._type == 3) {
                rowHtml = rowHtml + `<td>
                                <input type="date" name="InvoiceDetails[${rowIndex}].Note" class="form-control checkIn" placeholder="checkIn" />
                            </td>
                            <td>
                                <input type="date" name="InvoiceDetails[${rowIndex}].Quantity" class="form-control checkOut" value="1"  />
                            </td>`;
            }

            rowHtml = rowHtml + `<td>
                                <input type="text" name="InvoiceDetails[${rowIndex}].Note" class="form-control note" placeholder="Note" />
                            </td>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].Quantity" class="form-control orderQty" value="1" min="1" />
                            </td>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].UnitPrice" class="form-control itemPrice" value="${unitPrice.toFixed(2)}" step="0.01" />
                            </td>
                            <td>
                                <input type="text" name="InvoiceDetails[${rowIndex}].Amount" class="form-control itemTotal" readonly value="0.00" />
                            </td>
                            <td>
                                <button type="button" class="btn btn-danger btn-sm removeItemBtn">X</button>
                            </td>
                        </tr>`;

            $("#invoiceItems tbody").append(rowHtml);

            var $newRow = $("#invoiceItems tbody tr").last();

            // Remove row
            $newRow.find(".removeItemBtn").on("click", (e) => {
                $(e.currentTarget).closest("tr").remove();
                this.CalculateTotals();
            });

            // Update row total on quantity or unit price change
            $newRow.find(".orderQty, .itemPrice").on("input", (e) => {
                this.UpdateRowTotal($newRow);
            });

            // Recalculate totals
            this.CalculateTotals();
        },
        BindItemSelection: function () {
            var self = this;
            $("#invoiceItems").on("change", ".orderItemSelect", function () {

                var $row = $(this).closest("tr");
                var selected = $(this).find("option:selected");
                var price = parseFloat(selected.data("price")) || 0;
                var name = selected.text();

                $row.find(".itemId").val(selected.val());

                if (selected.val() > 0) {
                    $row.find(".description").val(name);
                }
                else {
                    $row.find(".description").val('');
                }
                $row.find(".itemPrice").val(price.toFixed(2));

                self.UpdateRowTotal($row);
            });
        },
        UpdateRowTotal: function (row) {
            var qty = parseFloat(row.find(".orderQty").val()) || 0;
            var price = parseFloat(row.find(".itemPrice").val()) || 0;
            var total = qty * price;
            row.find(".itemTotal").val(total.toFixed(2));
            this.CalculateTotals();
        },
        Save: function () {
            var self = this;

            // Validate before submitting
            if (!self.ValidateInvoice()) {
                return;
            }

            var grossAmount = parseFloat($("#grossAmount").html()) || 0;
            var alreadyPaid = self._invoice.paid;

            var cash = parseFloat($("#txtCash").val()) || 0;
            var balance = grossAmount - alreadyPaid;
            var change = cash - balance;

            if (change < 0) {
                change = 0;
            }

            var invoice = {
                InvoiceNo: $("#InvoiceNo").val(),
                Date: $("#Date").val(),
                Type: $("#Type").val(),
                Currency: $("#ddlCurrency").val(),
                CurrencyRate: parseFloat($("#txtCurrencyRate").val()) || 1,
                Status: $("#Status").val(),
                ReferenceNo: $("#ReferenceNo").val(),
                CustomerId: $("#CustomerId").val(),
                Note: $("#Note").val(),
                CurySubTotal: parseFloat($("#curySubTotal").html()) || 0,
                SubTotal: parseFloat($("#subTotal").html()) || 0,
                ServiceCharge: parseFloat($("#serviceCharge").html()) || 0,
                GrossAmount: grossAmount,
                Paid: parseFloat($("#txtPayment").val()),
                Cash: cash,
                Balance: balance,
                Change: change,
                PaymentType: parseInt($("#PaymentType").val()) || 1,
                PaymentReference: $("#txtPaymentReference").val(),
                InvoiceDetails: []
            };

            $("#invoiceItems tbody tr").each(function () {
                var $row = $(this);
                invoice.InvoiceDetails.push({
                    ItemId: parseInt($row.find(".itemId").val()),
                    Note: $row.find(".note").val(),
                    CheckIn: $row.find(".checkIn").val(),
                    CheckOut: $row.find(".checkOut").val(),
                    Quantity: parseInt($row.find(".orderQty").val()) || 0,
                    UnitPrice: parseFloat($row.find(".itemPrice").val()) || 0,
                    Amount: parseFloat($row.find(".itemTotal").val()) || 0
                });
            });

            var url = "/api/InvoicesApi/Save";

            //if (self._mode === "Edit") {
            //    var url = "/api/InvoicesApi/update"
            //}

            // Call the API
            $.ajax({
                url: url,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(invoice),
                success: function (res) {

                    if (res.success) {
                        alert("Invoice created successfully! No: " + res.invoice.invoiceNo);

                        self._invoice = res.invoice;
                        self.LoadInvoice();

                        if (self._mode === "Insert") {
                            history.pushState(null, "", "/Internal/Invoices/Edit/" + res.invoice.invoiceNo);
                            //window.location.href = "/Internal/Invoices/Edit/" + res.invoice.invoiceNo;
                        }
                    }
                },
                error: function (err) {
                    console.error(err);
                    alert("Error creating invoice. Check console for details.");
                }
            });
        },
        ValidateInvoice: function () {
            let isValid = true;
            let errors = [];

            //if (!$("#ReferenceNo").val()) {
            //    alert("Reference No is required.");
            //    return;
            //}
            if (!$("#CustomerId").val()) {
                $("#CustomerId").addClass("is-invalid");
                alert("Customer is required.");
                isValid = false;
                return false;
            }
            else {
                $("#CustomerId").removeClass("is-invalid");
            }

            var grossAmount = parseFloat($("#grossAmount").html());
            if (grossAmount <= 0) {
                alert("Gross amount must be greater than zero.");
                isValid = false;
                return false;
            }
            //if ($("#invoiceDetailsTable tbody tr").length === 0) {
            //    alert("Please add at least one invoice detail.");
            //   isValid = false;
            //}

            var paidAmount = parseFloat($("#txtPayment").val());
            var balanceAmount = parseFloat($("#txtBalance").html());

            if ($("#InvoiceNo").val() == 0) {
                balanceAmount = grossAmount;
            }

            if (paidAmount > balanceAmount) {
                if ($("#InvoiceNo").val() == 0) {
                    alert("Paid amount must be less than or equals to Gross Amount");
                } else {
                    alert("Paid amount must be less than or equals to Balance Amount");
                }

                isValid = false;
                return false;
            }

            if (self._type == 3) {
                // Check each invoice detail row
                $("#invoiceItems tbody tr").each(function (index) {
                    const itemId = $(this).find(".orderItemSelect").val();
                    const checkIn = $(this).find(".checkIn").val();
                    const checkOut = $(this).find(".checkOut").val();

                    if (!itemId) {
                        $(this).find(".orderItemSelect").addClass("is-invalid");
                        alert('Invalid item slected');
                        isValid = false;
                        return false;
                    }
                    else {
                        $(this).find(".orderItemSelect").removeClass("is-invalid");
                    }

                    if (!checkIn || !checkOut) {
                        $(this).find(".checkIn, .checkOut").addClass("is-invalid");
                        alert('Invalid check-in & check-out dates');
                        isValid = false;
                        return false;
                    }
                    else {
                        $(this).find(".checkIn, .checkOut").removeClass("is-invalid");
                    }

                    if (checkIn && checkOut) {
                        const inDate = new Date(checkIn);
                        const outDate = new Date(checkOut);

                        if (inDate > outDate) {
                            isValid = false;
                            errors.push(`Row ${index + 1}: Check-In cannot be after Check-Out.`);
                            $(this).find(".checkIn, .checkOut").addClass("is-invalid");
                            alert('Invalid check-in & check-out dates');
                        } else {
                            $(this).find(".checkIn, .checkOut").removeClass("is-invalid");
                        }
                    }
                });
            }

            // Validate payment reference when method requires it
            var method = parseInt($("#PaymentType").val());
            if (method === 2 || method === 3) {
                var pref = $("#txtPaymentReference").val();
                if (!pref || pref.trim().length === 0) {
                    alert("Payment Reference is required for selected method.");
                    return false;
                }
            }

            return isValid;
        },
        CalculateTotals: function () {
            var self = this;

            self.CalculateCurrySubTotal();
        },
        CalculateCurrySubTotal: function () {
            var self = this;

            const selectedCurrency = $("#ddlCurrency").val();

            var subtotal = 0;
            $("#invoiceItems tbody tr").each(function () {
                subtotal += parseFloat($(this).find(".itemTotal").val()) || 0;
            });
            $("#curySubTotal").text(subtotal.toFixed(2));
            $("#curySubTotal_code").text(selectedCurrency);

            self.CalculateGrossTotal();
        },
        CalculateGrossTotal: function () {
            var self = this;

            var curySubTotal = $("#curySubTotal").html();
            const selectedCurrency = $("#ddlCurrency").val();
            const currencyRate = parseFloat($("#txtCurrencyRate").val()) || 1;
            const subTotal = self.ConvertCurrency(curySubTotal, selectedCurrency, self._baseCurrency, currencyRate);

            var serviceCharge = 0;
            //Service charges applyes to Dining only
            if ($("#Type").val() == 1) {
                serviceCharge = subTotal * self.serviceCharge;
            }

            var grossTotal = parseFloat(subTotal) + parseFloat(serviceCharge);

            $("#subTotal").html(subTotal);
            $("#serviceCharge").html(serviceCharge.toFixed(2));
            $("#grossAmount").html(grossTotal.toFixed(2));
        },
        CalculateBalanceDue: function () {
            var self = this;

            var grossAmount = parseFloat($("#grossAmount").html());
            var balance = parseFloat($("#txtBalance").html());
            var cash = parseFloat($("#txtCash").val());
            $("#txtCash").val(cash.toFixed(2));

            var payment = 0;
            var balanceDue = 0

            if (cash >= balance) {
                payment = balance;
                balanceDue = payment - cash;
            }
            else {
                payment = cash;
                balanceDue = balance - cash;
            }

            $("#txtPayment").val(payment.toFixed(2));
            $("#txtBalanceDue").html(balanceDue.toFixed(2));
        },
        //FindRate: function (from, to) {
        //    var self = this;

        //    if (from === to) return 1;
        //    const direct = self._rates.find(r => r.from === from && r.to === to);
        //    if (direct) return direct.rate;

        //    // Try indirect conversion via base currency
        //    const base = self._baseCurrency;
        //    const viaBase1 = self._rates.find(r => r.from === from && r.to === base);
        //    const viaBase2 = self._rates.find(r => r.from === base && r.to === to);
        //    if (viaBase1 && viaBase2) {
        //        return viaBase1.rate * viaBase2.rate;
        //    }

        //    console.warn(`⚠️ No rate found for ${from} → ${to}`);
        //    return 1;
        //},
        ConvertCurrency: function (amount, from, to, customRate) {
            var self = this;

            if (customRate && customRate !== 1) {
                return amount * customRate;
            }
            return amount;

            //const rate = self.FindRate(from, to);
            //return amount * rate;
        },
        RemoveItemRow: function (row) {
            row.remove();
            this.CalculateTotals();
        }
    });
})(jQuery);
