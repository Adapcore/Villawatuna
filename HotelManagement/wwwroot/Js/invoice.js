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

        this.Init();
        return this;
    };

    $.invoice.fn = $.invoice.prototype = { version: '1.0.0' };
    $.invoice.fn.extend = $.invoice.extend = $.extend;

    $.invoice.fn.extend({
        Init: function () {
            this.BindEvents();
            //this.LoadInvoices();
            this.LoadItems();
            this.BindItemSelection();
            this.LoadServiceCharge();
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
                self.Save();
            });
        },

        BuildInvoice: function () {
            var invoice = {
                Id: $("#Id").val(),
                Date: $("#Date").val(),
                Type: $("#Type").val(),
                ReferenceNo: $("#ReferenceNo").val(),
                CustomerId: $("#CustomerId").val(),
                Note: $("#Note").val(),
                Status: $("#Status").val(),
                SubTotal: parseFloat($("#subTotal").val()) || 0,
                ServiceCharge: parseFloat($("#serviceCharge").val()) || 0,
                GrossAmount: parseFloat($("#grossAmount").val()) || 0,
                InvoiceDetails: []
            };

            $("#invoiceItems tbody tr").each(function () {
                var $row = $(this);
                var itemId = $row.find(".orderItemSelect").val();
                if (itemId) {
                    invoice.InvoiceDetails.push({
                        Id: $row.find(".detailId").val(),
                        ItemId: parseInt(itemId),
                        CheckIn: $(".checkIn").val(),
                        CheckOut: $(".checkOut").val(),
                        Note: $row.find(".orderNote").val(),
                        Quantity: parseInt($row.find(".orderQty").val()) || 0,
                        UnitPrice: parseFloat($row.find(".orderPrice").val()) || 0,
                        Amount: parseFloat($row.find(".itemTotal").val()) || 0
                    });
                }
            });
            return invoice;
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
                    self.itemOptions = data.map(i => `<option value="${i.id}">${i.name}</option>`).join('');
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
                Status: $("#Status").val(),
                ReferenceNo: $("#ReferenceNo").val(),
                CustomerId: $("#CustomerId").val(),
                Note: $("#Note").val(),
                SubTotal: parseFloat($("#subTotal").html()) || 0,
                ServiceCharge: parseFloat($("#serviceCharge").html()) || 0,
                GrossAmount: parseFloat($("#grossAmount").html()) || 0,
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

            var url = "/api/InvoicesApi/Create";

            if (self._mode === "Edit") {
                var url = "/api/InvoicesApi/update"
            }

            // Call the API
            $.ajax({
                url: url,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(invoice),
                success: function (res) {
                    if (res.success) {
                        alert("Invoice created successfully! No: " + res.invoiceNo);
                        window.location.href = "/Internal/Invoices";
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
            if (parseFloat($("#grossAmount").html()) <= 0) {
                alert("Gross amount must be greater than zero.");
                isValid = false;
                return false;
            }
            //if ($("#invoiceDetailsTable tbody tr").length === 0) {
            //    alert("Please add at least one invoice detail.");
            //   isValid = false;
            //}

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

            return isValid;
        },

        CalculateTotals: function () {
            var self = this;

            var subtotal = 0;
            $("#invoiceItems tbody tr").each(function () {
                subtotal += parseFloat($(this).find(".itemTotal").val()) || 0;
            });

            var serviceCharge = 0;
            //Service charges applyes to Dining only
            if ($("#Type").val() == 1) {
                serviceCharge = subtotal * self.serviceCharge;
            }

            var grossTotal = subtotal + serviceCharge;

            $("#subTotal").html(subtotal.toFixed(2));
            $("#serviceCharge").html(serviceCharge.toFixed(2));
            $("#grossAmount").html(grossTotal.toFixed(2));
        },
        RemoveItemRow: function (row) {
            row.remove();
            this.CalculateTotals();
        }
    });
})(jQuery);
