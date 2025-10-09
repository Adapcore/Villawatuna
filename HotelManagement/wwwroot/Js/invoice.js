(function ($) {
    $.fn.invoice = function (options) {
        return new $.invoice(this, options);
    };

    $.invoice = function (el, options) {
        var defaults = {
            serviceChargeRate: 0.1, // 10% by default
            createUrl: "/Invoices/Create"
        };

        this.options = $.extend(defaults, options);
        this.$container = $(el);
        this.itemIndex = 0;

        this.Init();
        return this;
    };

    $.invoice.fn = $.invoice.prototype = { version: '1.0.0' };
    $.invoice.fn.extend = $.invoice.extend = $.extend;

    $.invoice.fn.extend({
        Init: function () {
            this.BindEvents();
            //this.LoadInvoices();
        },
        BindEvents: function () {
            var self = this;

            // Item Add
            $('#addItemBtn').on("click", () => {
                this.AddItemRow();
            });

            // Item Remove
            $('.removeItemBtn').on("click", (e) => {
                var row = $(e.currentTarget).closest("tr");
                this.RemoveItemRow(row);
            });

            // Quantity change
            $("#invoiceItems tbody").on("change", ".orderQty", function (e) {
                var row = $(e.currentTarget).closest("tr");
                self.UpdateRowTotal(row);
            });

            $("#btnCreateInvoice").on("click", function () {

                // basic checks
                //if (!$("#ReferenceNo").val()) {
                //    alert("Reference No is required.");
                //    return;
                //}
                if (!$("#CustomerId").val()) {
                    alert("Customer is required.");
                    return;
                }
                if (parseFloat($("#grossAmount").val()) <= 0) {
                    alert("Gross amount must be greater than zero.");
                    return;
                }
                //if ($("#invoiceDetailsTable tbody tr").length === 0) {
                //    alert("Please add at least one invoice detail.");
                //    return;
                //}

                var invoice = {
                    Date: $("#Date").val(),
                    Type: $("#Type").val(),
                    ReferenceNo: $("#ReferenceNo").val(),
                    CustomerId: $("#CustomerId").val(),
                    Note: $("#Note").val(),
                    SubTotal: parseFloat($("#subTotal").val()) || 0,
                    ServiceCharge: parseFloat($("#serviceCharge").val()) || 0,
                    GrossAmount: parseFloat($("#grossAmount").val()) || 0,
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

                // Call the API
                $.ajax({
                    url: "/api/InvoicesApi/Create",
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(invoice),
                    success: function (res) {
                        if (res.success) {
                            alert("Invoice created successfully! No: " + res.invoiceNo);
                            window.location.href = "/Invoices/Index";
                        }
                    },
                    error: function (err) {
                        console.error(err);
                        alert("Error creating invoice. Check console for details.");
                    }
                });
            });

        },
        
        AddItemRow: function (item) {
            // item is an optional object: { Id, Description, UnitPrice } 
            var rowIndex = $("#invoiceItems tbody tr").length;

            var itemId = item?.Id || 0;
            var description = item?.Description || '';
            var unitPrice = item?.UnitPrice || 0.00;

            var rowHtml = `
                        <tr>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].ItemId" class="form-control itemId" value="${itemId}" />
                            </td>
                            <td>
                                <input type="text" name="InvoiceDetails[${rowIndex}].Description" class="form-control description" value="${description}" readonly />
                            </td>
                            <td>
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
                        </tr>
                    `;

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


        UpdateRowTotal: function (row) {
            var qty = parseFloat(row.find(".orderQty").val()) || 0;
            var price = parseFloat(row.find(".itemPrice").val()) || 0;
            var total = qty * price;
            row.find(".itemTotal").val(total.toFixed(2));
            this.CalculateTotals();
        },

        CalculateTotals: function () {

            var subtotal = 0;
            $("#invoiceItems tbody tr").each(function () {
                subtotal += parseFloat($(this).find(".itemTotal").val()) || 0;
            });

            var serviceCharge = 0;
            if ($("#Type").val() == 1) {
                serviceCharge = subtotal * 0.1; // 10% service charge 
            }

            var grossTotal = subtotal + serviceCharge;

            $("#subTotal").val(subtotal.toFixed(2));
            $("#serviceCharge").val(serviceCharge.toFixed(2));
            $("#grossAmount").val(grossTotal.toFixed(2));
        },
        RemoveItemRow: function (row) {
            row.remove();
            this.CalculateTotals();
        }
    });
})(jQuery);
