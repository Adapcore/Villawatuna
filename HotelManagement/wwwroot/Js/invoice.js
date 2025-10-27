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
        this._type = this.options.type;
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
            //this.LoadInvoices();
            this.LoadItems();
            this.BindItemSelection();
            this.LoadServiceCharge();

            // hardcoded rates (will later be replaced by API) 
            this._rates = [
                { 'from': 'LKR', 'to': 'USD', 'rate': 0.00300 },
                { 'from': 'USD', 'to': 'LKR', 'rate': 300 },

                { 'from': 'LKR', 'to': 'GBP', 'rate': 0.0026 },
                { 'from': 'GBP', 'to': 'LKR', 'rate': 380 },

                { 'from': 'LKR', 'to': 'EUR', 'rate': 0.0030 },
                { 'from': 'EUR', 'to': 'LKR', 'rate': 330 },

                { 'from': 'USD', 'to': 'GBP', 'rate': 0.78 },
                { 'from': 'GBP', 'to': 'USD', 'rate': 1.28 },
            ]

            if (self._mode == 'Edit') {
                $('#btn_print').removeClass('d-none');
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

            $("#btnCreateInvoice").on("click", function () {
                self.Save();
            });

            $("#Currency").on("change", function () {
                self.CalculateTotals();
            });
        },

        LoadServiceCharge: function () {
            var self = this;
            $.getJSON("/api/menu/GetServiceCharge", function (data) {
                self.serviceCharge = data;
            });
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

            var invoice = {
                InvoiceNo: $("#InvoiceNo").val(),
                Date: $("#Date").val(),
                Type: $("#Type").val(),
                Currency: $("#Currency").val(),
                Status: $("#Status").val(),
                ReferenceNo: $("#ReferenceNo").val(),
                CustomerId: $("#CustomerId").val(),
                Note: $("#Note").val(),
                CurySubTotal: parseFloat($("#curySubTotal").html()) || 0,
                SubTotal: parseFloat($("#subTotal").html()) || 0,
                ServiceCharge: parseFloat($("#serviceCharge").html()) || 0,
                GrossAmount: parseFloat($("#grossAmount").html()) || 0,
                Paid: parseFloat($("#Paid").val()),
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
                        self.UpdateInvoice(res.invoice);
                        alert("Invoice created successfully! No: " + res.invoice.invoiceNo);

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

            var paidAmount = parseFloat($("#Paid").val());
            var balanceAmount = parseFloat($("#Balance").html());

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

            return isValid;
        },

        CalculateTotals: function () {
            var self = this;

            self.CalculateCurrySubTotal();
        },

        CalculateCurrySubTotal: function () {
            var self = this;

            const selectedCurrency = $("#Currency").val();

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
            const selectedCurrency = $("#Currency").val();
            const subTotal = self.ConvertCurrency(curySubTotal, selectedCurrency, self._baseCurrency);

            var serviceCharge = 0;
            //Service charges applyes to Dining only
            if ($("#Type").val() == 1) {
                serviceCharge = subTotal * self.serviceCharge;
            }

            var grossTotal = subTotal + serviceCharge;

            $("#subTotal").html(subTotal.toFixed(2));
            $("#serviceCharge").html(serviceCharge.toFixed(2));
            $("#grossAmount").html(grossTotal.toFixed(2));
        },

        UpdateInvoice: function (invoice) {
            var self = this;

            $("#InvoiceNo").val(invoice.invoiceNo);
            $('#Balance').html(invoice.balance)
            $('#dv_balance').removeClass('d-none');
            $('#btn_print').removeClass('d-none');
            $("#Paid").val(0);
        },

        FindRate: function (from, to) {
            var self = this;

            if (from === to) return 1;
            const direct = self._rates.find(r => r.from === from && r.to === to);
            if (direct) return direct.rate;

            // Try indirect conversion via base currency
            const base = self._baseCurrency;
            const viaBase1 = self._rates.find(r => r.from === from && r.to === base);
            const viaBase2 = self._rates.find(r => r.from === base && r.to === to);
            if (viaBase1 && viaBase2) {
                return viaBase1.rate * viaBase2.rate;
            }

            console.warn(`⚠️ No rate found for ${from} → ${to}`);
            return 1;
        },

        ConvertCurrency: function (amount, from, to) {
            var self = this;

            const rate = self.FindRate(from, to);
            return amount * rate;
        },

        RemoveItemRow: function (row) {
            row.remove();
            this.CalculateTotals();
        }
    });
})(jQuery);
