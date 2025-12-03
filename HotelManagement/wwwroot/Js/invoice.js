(function ($) {
    $.fn.invoice = function (options) {
        return new $.invoice(this, options);
    };

    $.invoice = function (el, options) {
        var defaults = {
            mode: 'Insert',
            createUrl: "/Invoices/Create",
            isAdmin: false
        };

        this.options = $.extend(defaults, options);
        this._mode = this.options.mode;
        this._invoice = this.options.invoice;
        this._type = this.options.invoice.type;
        this.$container = $(el);
        this.itemIndex = 0;
        this.serviceCharge = 0;
        this._baseCurrency = 'LKR';
        this.curySubTotal = 0;
        this.subTotal = 0;
        this.grossTotal = 0;
        this._currentRow = null;
        this._isAdmin = this.options.isAdmin || false;
        this._currencyData = this.options.currencyData || [];
        this._isSaving = false; // Flag to track if save is in progress

        this._formatter = new Intl.NumberFormat('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });

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
            
            // Initialize Select2 on existing dropdowns after a short delay to ensure DOM is ready
            setTimeout(function() {
                self.InitializeSelect2();
            }, 100);

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

            self.curySubTotal = self._invoice.curySubTotal;
            self.subTotal = self._invoice.subTotal;
            self.grossTotal = self._invoice.grossAmount;

            $("#InvoiceNo").val(self._invoice.invoiceNo);
            $("#Status").val(self._invoice.status);
            //$('#txtPaid').html(self._invoice.paid.toFixed(2));
            $('#txtPaid').html(self._formatter.format(self._invoice.paid));

            $('#txtBalance').html(self._formatter.format(self._invoice.balance));
            $('#txtChange').html(self._formatter.format(self._invoice.change));
            $('#txtLastPaid').html(self._formatter.format(self._invoice.cash));
            $('#txtChange').html(self._formatter.format(self._invoice.change));

            $("#txtPayment").val(0);
            $("#txtCash").val(0);
            $("#txtBalanceDue").html(self._formatter.format(self._invoice.balance));

            if (self._invoice.type == 3 || self._invoice.type == 2) {
                $("#btn_print").attr("href", "/Internal/Invoices/Print/" + self._invoice.invoiceNo);
            }
            else {
                $("#btn_print").attr("href", "/Internal/Invoices/PrintThermal/" + self._invoice.invoiceNo);
            }

            // Load currency rate if available
            if (self._invoice.currencyRate) {
                $("#txtCurrencyRate").val(self._invoice.currencyRate);
            }
            else {
                self.LoadCurrencyRate();
            }

            $("#PaymentType").val(1);
            $("#txtPaymentReference").val('');
            $("#dv_paymentRef").hide();


            $('#dv_paidWrapper').hide();
            $('#dv_changeWrapper').addClass('d-none');
            $('#dv_paymentWrapper').hide();
            $('#dv_balanceWrapper').hide();

            $('#btnComplete').hide();
            $('#btnPay').hide();
            $('#btn_print').hide();
            $('#btnReOpen').hide();

            self.DisableHeader();
            self.DisableDetails();

            if (self._invoice.status == 1) { // In-Progress
                self.EnableHeader(0);

                $('#btnComplete').show();
                // Bind click event when button is shown
                $('#btnComplete').off('click').on('click', function () {
                    // Show jQuery confirmation dialog
                    showConfirmDialog("Do you want to complete the invoice?", function(confirmed) {
                        if (confirmed) {
                            $("#Status").val(2);
                            self.Save();
                        }
                    },
                        {
                            type: 'Warning'
                        }
                    );
                });

                self.EnableDetails();
            }
            else if (self._invoice.status == 2) { // Complete
                self.EnableHeader(1);

                // Show reopen button only for admin users
                if (self._isAdmin) {
                    $('#btnReOpen').show();
                    // Bind click event when button is shown
                    $('#btnReOpen').off('click').on('click', function () {
                        $("#Status").val(1);
                        self.Save();
                    });
                }              
                $('#btnPay').show();
                $('#btn_print').show();
            }
            else if (self._invoice.status == 3) { // Partally Paid
                self.EnableHeader(1);
                $('#dv_paidWrapper').show();
                $('#dv_paymentWrapper').slideDown();
                $('#dv_balanceWrapper').slideDown();
                $('#btn_print').show();
            }
            else if (self._invoice.status == 4) { // Paid
                $('#dv_paidWrapper').show();                
                $('#dv_changeWrapper').removeClass('d-none');
                $('#btnSave').hide();
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

            // Price change
            $("#invoiceItems tbody").on("change", ".itemPrice", function (e) {
                var row = $(e.currentTarget).closest("tr");
                self.UpdateRowTotal(row);
            });

            $("#invoiceItems tbody").on("change", ".itemTotal", function (e) {
                var row = $(e.currentTarget).closest("tr");
                self.CalculateTotals();
            });

            $("#invoiceItems tbody").on("change", ".checkIn, .checkOut", function (e) {
                var row = $(e.currentTarget).closest("tr");
                self.CalculateNights(row);
            });

            // Clear note validation error when user types
            $("#invoiceItems tbody").on("input change", ".note", function (e) {
                var $noteField = $(this);
                var noteValue = $noteField.val();
                
                // If note has value, remove validation error
                if (noteValue && noteValue.trim().length > 0) {
                    $noteField.removeClass("is-invalid");
                }
            });

            // Clear validation errors for Qty, Price, and Amount when user types
            $("#invoiceItems tbody").on("input change", ".orderQty, .itemPrice, .itemTotal", function (e) {
                var $field = $(this);
                var fieldValue = parseFloat($field.val()) || 0;
                
                // Remove validation error if field has valid value
                if (fieldValue > 0 || ($field.hasClass("itemTotal") && fieldValue >= 0)) {
                    $field.removeClass("is-invalid");
                }
            });

            // btnComplete and btnReOpen click handlers are bound when buttons are shown (in LoadInvoice function)

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

            $("#btnPayBalance").on("click", function () {
                var balance = self.ParseNumber($("#txtBalance").html());
                $("#txtCash").val(balance.toFixed(2));
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

            $(".btnAddItem").on("click", function () {
                self._currentRow = $(this).closest("tr");   // store the row
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
            $('#dv_balanceWrapper').slideDown();
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
            var toCurrency = self._baseCurrency || "LKR";

            if (fromCurrency && fromCurrency !== toCurrency) {
                // First, try to get rate from Umbraco currency data
                var umbracoRate = null;
                if (self._currencyData && self._currencyData.length > 0) {
                    var selectedCurrency = self._currencyData.find(function(c) {
                        return c.code === fromCurrency;
                    });
                    if (selectedCurrency && selectedCurrency.exchangeRate) {
                        umbracoRate = selectedCurrency.exchangeRate;
                    }
                }

                if (umbracoRate !== null && umbracoRate > 0) {
                    // Use Umbraco rate
                    $("#txtCurrencyRate").val(umbracoRate);
                    self.CalculateTotals();
                }
                else {
                    // Fallback to API (keep existing API logic for later connection)
                    $.getJSON("/api/currency/getCurrencyRate", { from: fromCurrency, to: toCurrency })
                        .done(function (data) {
                            $("#txtCurrencyRate").val(data.rate);
                            self.CalculateTotals();
                        })
                        .fail(function () {
                            $("#txtCurrencyRate").val("");
                            console.warn("Failed to load currency rate");
                        });
                }
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

                    let allItems = [];

                    // Flatten the nested structure
                    data.forEach(function (cat) {
                        cat.items.forEach(function (itm) {
                            allItems.push({
                                id: itm.id,
                                name: itm.name,
                                price: itm.price,
                                category: cat.category,
                                noteRequired: itm.noteRequired || false
                            });
                        });
                    });
                    
                    // Store items data for validation
                    self._itemsData = {};
                    allItems.forEach(function (item) {
                        self._itemsData[item.id] = item;
                    });

                    let html = "";

                    // -------------------------------------
                    // CATEGORY-WISE ITEMS
                    // -------------------------------------
                    let groups = {};

                    allItems.forEach(function (i) {
                        if (!groups[i.category]) groups[i.category] = [];
                        groups[i.category].push(i);
                    });

                    $.each(groups, function (catName, items) {
                        html += `<optgroup label="${catName}">`;
                        html += items.map(i =>
                            `<option value="${i.id}" data-price="${i.price}" data-note-required="${i.noteRequired || false}">${i.name}</option>`
                        ).join('');
                        html += `</optgroup>`;
                    });

                    self.itemOptions = html;

                    //self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                    self.BindItemsToPopUp(data);
                });
            }
            else if (self._type == 3)// Stay
            {
                $.getJSON("/api/room/GetRoomCategories", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="0">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                    self.BindItemsToPopUp(data);
                });
            }
            else if (self._type == 4)// Other Types
            {
                $.getJSON("/api/otherType/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                    self.BindItemsToPopUp(data);
                });
            }
            else if (self._type == 6)// Laundry 
            {
                $.getJSON("/api/laundry/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                    self.BindItemsToPopUp(data);
                });
            }
            else if (self._type == 5)// Tour Types
            {
                $.getJSON("/api/tourType/GetItems", function (data) {
                    self.itemOptions = data.map(i => `<option value="${i.id}" data-price="${i.price}">${i.name}</option>`).join('');
                    self.SelectDropDownValue();
                    self.BindItemsToPopUp(data);
                });
            }
        },
        BindItemsToPopUp: function (data) {
            var self = this;

            const $container = $("#itemContainer");
            $container.empty();

            // Search input HTML
            let searchHtml = `
                <div class="modal-body p-3" style="flex: 0 0 auto;">
                    <div class="mb-3">
                        <div class="input-group input-group-lg">
                            <span class="input-group-text bg-primary text-white">
                                <i class="bi bi-search"></i>
                            </span>
                            <input type="text" id="itemSearchInput" class="form-control form-control-lg" 
                                   placeholder="Search items..." autocomplete="off" />
                            <button type="button" id="clearSearchBtn" class="btn btn-outline-secondary d-none">
                                <i class="bi bi-x-circle"></i>
                            </button>
                        </div>
                    </div>
                </div>
                <div class="modal-body p-3" style="flex: 1 1 auto; overflow-y: auto;">
                    <div id="itemsGrid" class="row g-3"></div>
                    <div id="noItemsFound" class="text-center text-muted py-5 d-none">
                        <i class="bi bi-inbox fs-1"></i>
                        <p class="mt-3 fs-5">No items found</p>
                    </div>
                </div>
            `;

            $container.html(searchHtml);

            // Store original data for filtering
            self._allItemsData = data;

            // Render items
            self.RenderItems(data);

            // Bind search functionality
            $('#itemSearchInput').on('input', function() {
                const searchTerm = $(this).val().toLowerCase().trim();
                const $clearBtn = $('#clearSearchBtn');
                
                if (searchTerm.length > 0) {
                    $clearBtn.removeClass('d-none');
                } else {
                    $clearBtn.addClass('d-none');
                }

                self.FilterItems(searchTerm);
            });

            $('#clearSearchBtn').on('click', function() {
                $('#itemSearchInput').val('').trigger('input');
            });

            // Focus search on modal show
            $('#addItemModal').on('shown.bs.modal', function() {
                $('#itemSearchInput').focus();
            });

            self.BindItemCardEvents();
        },

        RenderItems: function(data) {
            var self = this;
            const $grid = $("#itemsGrid");
            $grid.empty();

            // Case 1: Flat list of items (NO category)
            const isFlatList = data.length > 0 && data[0].id !== undefined;

            if (isFlatList) {
                if (data && data.length > 0) {
                    $.each(data, function (i, item) {
                        const itemHtml = self.CreateItemCard(item, i);
                        $grid.append(itemHtml);
                    });
                } else {
                    $grid.html(`
                        <div class="col-12">
                            <div class="text-center text-muted py-5">
                                <i class="bi bi-inbox fs-1"></i>
                                <p class="mt-3 fs-5">No items available</p>
                            </div>
                        </div>
                    `);
                }
                return;
            }

            // Case 2: With categories → Tabs
            let tabButtons = `<ul class="nav nav-tabs nav-tabs-lg mb-3" style="flex-wrap: wrap;">`;
            let tabContent = `<div class="tab-content">`;

            $.each(data, function (index, cat) {
                let active = index === 0 ? "active" : "";
                let show = index === 0 ? "show active" : "";

                tabButtons += `
            <li class="nav-item">
                <button class="nav-link ${active} px-3 py-2" data-bs-toggle="tab"
                        data-bs-target="#tab${index}" type="button" style="font-size: 1rem; min-height: 48px;">
                    ${cat.category}
                </button>
            </li>`;

                tabContent += `
            <div class="tab-pane fade ${show}" id="tab${index}">
                <div class="row g-3">`;

                if (cat.items && cat.items.length > 0) {
                    $.each(cat.items, function (i, item) {
                        tabContent += self.CreateItemCard(item, i);
                    });
                } else {
                    tabContent += `
                    <div class="col-12">
                        <div class="text-center text-muted py-5">
                            <i class="bi bi-inbox fs-1"></i>
                            <p class="mt-3 fs-5">No items available</p>
                        </div>
                    </div>`;
                }

                tabContent += `</div></div>`;
            });

            tabButtons += `</ul>`;
            tabContent += `</div>`;

            $grid.html(tabButtons + tabContent);
        },

        CreateItemCard: function(item, index) {
            var self = this;
            // Different gradient colors for visual variety
            const gradients = [
                'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
                'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
                'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
                'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
                'linear-gradient(135deg, #30cfd0 0%, #330867 100%)',
                'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
                'linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%)'
            ];
            // Use item ID to determine gradient for consistent coloring per item
            // This breaks the row pattern by using unique item IDs instead of position index
            const gradientIndex = (item.id || index || 0) % gradients.length;
            const gradient = gradients[gradientIndex];
            const baseCurrency = self._baseCurrency || 'LKR';

            const noteRequired = item.noteRequired || false;
            return `
                <div class="col-3 col-lg-8per-row itemCardWrapper" 
                     data-id="${item.id}"
                     data-name="${item.name.toLowerCase()}"
                     data-price="${item.price}">
                    <div class="card shadow-sm h-100 border-0 itemCard" 
                         style="cursor: pointer; transition: all 0.2s ease; background: ${gradient};"
                         data-id="${item.id}"
                         data-name="${item.name}"
                         data-price="${item.price}"
                         data-note-required="${noteRequired}">
                        <div class="card-body text-center p-2 d-flex flex-column justify-content-center" 
                             style="min-height: 80px;">
                            <h6 class="fw-bold text-white mb-1" style="font-size: 0.85rem; line-height: 1.2;">
                                ${item.name}
                            </h6>
                            <div class="text-white fw-bold mt-auto" style="font-size: 0.9rem;">
                                ${baseCurrency} ${item.price.toLocaleString()}
                            </div>
                        </div>
                    </div>
                </div>`;
        },

        FilterItems: function(searchTerm) {
            var self = this;
            const $grid = $("#itemsGrid");
            const $noItems = $("#noItemsFound");
            let visibleCount = 0;

            if (!searchTerm || searchTerm.length === 0) {
                // Show all items
                $('.itemCardWrapper').removeClass('d-none');
                $noItems.addClass('d-none');
                return;
            }

            // Filter items
            $('.itemCardWrapper').each(function() {
                const $wrapper = $(this);
                const itemName = $wrapper.data('name') || '';
                const itemId = $wrapper.data('id') || '';
                
                if (itemName.includes(searchTerm) || itemId.toString().includes(searchTerm)) {
                    $wrapper.removeClass('d-none');
                    visibleCount++;
                } else {
                    $wrapper.addClass('d-none');
                }
            });

            // Show/hide "no items" message
            if (visibleCount === 0) {
                $noItems.removeClass('d-none');
            } else {
                $noItems.addClass('d-none');
            }
        },

        BindItemCardEvents: function () {
            var self = this;

            // Use event delegation for dynamically added cards
            $(document).off("click", ".itemCard").on("click", ".itemCard", function (e) {
                e.preventDefault();
                e.stopPropagation();

                if (!self._currentRow) return;

                const itemId = $(this).data("id");
                const itemName = $(this).data("name");
                const itemPrice = $(this).data("price");
                const noteRequired = $(this).data("note-required") === true || $(this).data("note-required") === "true";

                // Find the dropdown inside that row
                const $ddl = self._currentRow.find(".orderItemSelect");

                // Add option if not exists
                if ($ddl.find(`option[value='${itemId}']`).length === 0) {
                    $ddl.append(`<option value="${itemId}" data-price="${itemPrice}" data-note-required="${noteRequired}">${itemName}</option>`);
                }

                // Select the item
                $ddl.val(itemId).trigger("change");

                // Set price automatically
                self._currentRow.find(".itemPrice").val(itemPrice);

                // Recalculate amount
                const qty = parseFloat(self._currentRow.find(".orderQty").val() || 1);
                const total = qty * itemPrice;
                self._currentRow.find(".itemTotal").val(total.toFixed(2));

                // Update totals
                self.UpdateRowTotal(self._currentRow);

                // Close modal
                const modalEl = document.getElementById('addItemModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) {
                    modal.hide();
                }

                // Clear search
                $('#itemSearchInput').val('').trigger('input');
            });
        },

        SelectDropDownValue: function () {
            var self = this;

            // Populate all selects in the table
            $("#invoiceItems tbody tr").each(function () {
                var $row = $(this);
                var $select = $row.find(".orderItemSelect");
                var selectedId = $row.find(".itemId").val(); // <-- pick from hidden field

                // Destroy Select2 if already initialized
                if ($select.hasClass("select2-hidden-accessible")) {
                    $select.select2('destroy');
                }

                $select.html('<option value="">-- Select --</option>' + self.itemOptions);

                if (selectedId) {
                    $select.val(selectedId); // set dropdown
                }

                // Set fixed width on select before initializing Select2
                $select.css({
                    'width': '250px',
                    'min-width': '250px',
                    'max-width': '250px'
                });

                // Initialize Select2 with fixed pixel width
                $select.select2({
                    theme: 'bootstrap-5',
                    width: '250px',
                    placeholder: 'Select item...',
                    allowClear: false,
                    dropdownParent: $select.closest('.modal, body')
                });

                // Force Select2 container to maintain fixed width
                setTimeout(function() {
                    var $select2Container = $select.next('.select2-container');
                    if ($select2Container.length) {
                        var containerEl = $select2Container[0];
                        // Set fixed width with !important using setProperty
                        containerEl.style.setProperty('width', '250px', 'important');
                        containerEl.style.setProperty('min-width', '250px', 'important');
                        containerEl.style.setProperty('max-width', '250px', 'important');
                        
                        // Also fix the selection width
                        $select2Container.find('.select2-selection').css({
                            'width': '100%',
                            'min-width': '100%',
                            'max-width': '100%'
                        });
                        
                        $select2Container.find('.select2-selection__rendered').css({
                            'width': '100%',
                            'max-width': '100%',
                            'overflow': 'hidden',
                            'text-overflow': 'ellipsis',
                            'white-space': 'nowrap'
                        });
                        
                        // Use MutationObserver to watch for width changes
                        var observer = new MutationObserver(function(mutations) {
                            var currentWidth = $select2Container.width();
                            if (Math.abs(currentWidth - 250) > 1) {
                                containerEl.style.setProperty('width', '250px', 'important');
                                containerEl.style.setProperty('min-width', '250px', 'important');
                                containerEl.style.setProperty('max-width', '250px', 'important');
                            }
                        });
                        
                        observer.observe(containerEl, {
                            attributes: true,
                            attributeFilter: ['style', 'class'],
                            childList: false,
                            subtree: false
                        });
                        
                        // Store observer on element for cleanup if needed
                        $select2Container.data('widthObserver', observer);
                    }
                }, 10);
                
                // Prevent width changes on all Select2 events - use a more aggressive approach
                $select.on('select2:select select2:unselect select2:open select2:close select2:selecting', function() {
                    var $select2Container = $(this).next('.select2-container');
                    if ($select2Container.length) {
                        setTimeout(function() {
                            var containerEl = $select2Container[0];
                            containerEl.style.setProperty('width', '250px', 'important');
                            containerEl.style.setProperty('min-width', '250px', 'important');
                            containerEl.style.setProperty('max-width', '250px', 'important');
                        }, 0);
                    }
                });

                // Adjust Select2 border radius to match input-group styling
                $select.on('select2:open', function() {
                    $(this).closest('.input-group').find('.select2-container--bootstrap-5 .select2-selection').css({
                        'border-top-right-radius': '0',
                        'border-bottom-right-radius': '0',
                        'border-right': 'none'
                    });
                });
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
                                <input type="hidden" value="0" class="itemId" />
                                <div class="input-group input-group-sm">
                                <select class="form-select orderItemSelect">
                                    <option value="">-- Select --</option>
                                    ${this.itemOptions || ""}
                                </select>
                                 <button type="button" class="btnAddItem btn btn-primary" data-bs-toggle="modal" data-bs-target="#addItemModal" title="Browse Items">
                                    <i class="bi bi-grid-3x3-gap"></i>
                                </button>
                                </div>
                            </td>`;

            if (self._type == 3) {
                // Get today's date in YYYY-MM-DD format
                var today = new Date();
                var todayStr = today.getFullYear() + '-' + 
                               String(today.getMonth() + 1).padStart(2, '0') + '-' + 
                               String(today.getDate()).padStart(2, '0');
                
                rowHtml = rowHtml + `<td>
                                <input type="date" name="InvoiceDetails[${rowIndex}].CheckIn" class="form-control form-control-sm checkIn" value="${todayStr}" />
                            </td>
                            <td>
                                <input type="date" name="InvoiceDetails[${rowIndex}].CheckOut" class="form-control form-control-sm checkOut" />
                            </td>`;
            }

            rowHtml = rowHtml + `<td>
                                <input type="text" name="InvoiceDetails[${rowIndex}].Note" class="form-control note" placeholder="Note" />
                            </td>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].Quantity" class="form-control orderQty text-end" value="1" min="1" />
                            </td>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].UnitPrice" class="form-control itemPrice text-end" value="${unitPrice.toFixed(2)}" step="0.01" />
                            </td>
                            <td>
                                <input type="number" name="InvoiceDetails[${rowIndex}].Amount" class="form-control itemTotal text-end" value="0.00" step="0.01" />
                            </td>
                            <td>
                                <button type="button" class="btn btn-danger btn-sm removeItemBtn">X</button>
                            </td>
                        </tr>`;

            $("#invoiceItems tbody").append(rowHtml);

            var $newRow = $("#invoiceItems tbody tr").last();

            // Initialize Select2 on the new dropdown with fixed width
            var $newSelect = $newRow.find(".orderItemSelect");
            $newSelect.css({
                'width': '250px',
                'min-width': '250px',
                'max-width': '250px'
            });
            $newSelect.select2({
                theme: 'bootstrap-5',
                width: '250px',
                placeholder: 'Select item...',
                allowClear: false,
                dropdownParent: $newSelect.closest('.modal, body')
            });
            
            // Force Select2 container to maintain fixed width
            setTimeout(function() {
                var $select2Container = $newSelect.next('.select2-container');
                if ($select2Container.length) {
                    var containerEl = $select2Container[0];
                    // Set fixed width with !important using setProperty
                    containerEl.style.setProperty('width', '250px', 'important');
                    containerEl.style.setProperty('min-width', '250px', 'important');
                    containerEl.style.setProperty('max-width', '250px', 'important');
                    
                    $select2Container.find('.select2-selection').css({
                        'width': '100%',
                        'min-width': '100%',
                        'max-width': '100%'
                    });
                    
                    $select2Container.find('.select2-selection__rendered').css({
                        'width': '100%',
                        'max-width': '100%',
                        'overflow': 'hidden',
                        'text-overflow': 'ellipsis',
                        'white-space': 'nowrap'
                    });
                    
                    // Use MutationObserver to watch for width changes
                    var observer = new MutationObserver(function(mutations) {
                        var currentWidth = $select2Container.width();
                        if (Math.abs(currentWidth - 250) > 1) {
                            containerEl.style.setProperty('width', '250px', 'important');
                            containerEl.style.setProperty('min-width', '250px', 'important');
                            containerEl.style.setProperty('max-width', '250px', 'important');
                        }
                    });
                    
                    observer.observe(containerEl, {
                        attributes: true,
                        attributeFilter: ['style', 'class'],
                        childList: false,
                        subtree: false
                    });
                    
                    // Store observer on element for cleanup if needed
                    $select2Container.data('widthObserver', observer);
                }
            }, 10);
            
            // Prevent width changes on all Select2 events - use a more aggressive approach
            $newSelect.on('select2:select select2:unselect select2:open select2:close select2:selecting', function() {
                var $select2Container = $(this).next('.select2-container');
                if ($select2Container.length) {
                    setTimeout(function() {
                        var containerEl = $select2Container[0];
                        containerEl.style.setProperty('width', '250px', 'important');
                        containerEl.style.setProperty('min-width', '250px', 'important');
                        containerEl.style.setProperty('max-width', '250px', 'important');
                    }, 0);
                }
            });

            // Force Select2 container to work within input-group
            setTimeout(function() {
                var $select2Container = $newSelect.next('.select2-container');
                if ($select2Container.length) {
                    $select2Container.css({
                        'width': 'auto',
                        'flex': '1 1 auto',
                        'min-width': '0',
                        'max-width': 'calc(100% - 50px)'
                    });
                }
            }, 10);

            // Adjust Select2 border radius to match input-group styling
            $newSelect.on('select2:open', function() {
                $(this).closest('.input-group').find('.select2-container--bootstrap-5 .select2-selection').css({
                    'border-top-right-radius': '0',
                    'border-bottom-right-radius': '0',
                    'border-right': 'none'
                });
            });

            // Remove row
            $newRow.find(".removeItemBtn").on("click", (e) => {
                // Destroy Select2 before removing
                $newRow.find(".orderItemSelect").select2('destroy');
                $(e.currentTarget).closest("tr").remove();
                this.CalculateTotals();
            });

            // Update row total on quantity or unit price change
            $newRow.find(".orderQty, .itemPrice").on("input", (e) => {
                this.UpdateRowTotal($newRow);
            });

            // Check-in and check-out date changes are handled by the delegated event handler in BindEvents
            // No need to bind here as it's already handled globally

            if (self._type == 3) {
                $newRow.find(".orderQty").prop('readonly', true);
            }

            $(".btnAddItem").off("click");
            $(".btnAddItem").on("click", function () {
                self._currentRow = $(this).closest("tr");   // store the row
            });

            // Recalculate totals
            this.CalculateTotals();
        },

        InitializeSelect2: function() {
            var self = this;
            // Initialize Select2 on all existing orderItemSelect dropdowns
            $(".orderItemSelect").each(function() {
                var $select = $(this);
                if (!$select.hasClass("select2-hidden-accessible")) {
                    // Set fixed width on select before initializing Select2
                    $select.css({
                        'width': '250px',
                        'min-width': '250px',
                        'max-width': '250px'
                    });
                    
                    $select.select2({
                        theme: 'bootstrap-5',
                        width: '250px',
                        placeholder: 'Select item...',
                        allowClear: false,
                        dropdownParent: $select.closest('.modal, body')
                    });

                    // Force Select2 container to maintain fixed width
                    setTimeout(function() {
                        var $select2Container = $select.next('.select2-container');
                        if ($select2Container.length) {
                            var containerEl = $select2Container[0];
                            // Set fixed width with !important using setProperty
                            containerEl.style.setProperty('width', '250px', 'important');
                            containerEl.style.setProperty('min-width', '250px', 'important');
                            containerEl.style.setProperty('max-width', '250px', 'important');
                            
                            $select2Container.find('.select2-selection').css({
                                'width': '100%',
                                'min-width': '100%',
                                'max-width': '100%'
                            });
                            
                            $select2Container.find('.select2-selection__rendered').css({
                                'width': '100%',
                                'max-width': '100%',
                                'overflow': 'hidden',
                                'text-overflow': 'ellipsis',
                                'white-space': 'nowrap'
                            });
                            
                            // Use MutationObserver to watch for width changes
                            var observer = new MutationObserver(function(mutations) {
                                var currentWidth = $select2Container.width();
                                if (Math.abs(currentWidth - 250) > 1) {
                                    containerEl.style.setProperty('width', '250px', 'important');
                                    containerEl.style.setProperty('min-width', '250px', 'important');
                                    containerEl.style.setProperty('max-width', '250px', 'important');
                                }
                            });
                            
                            observer.observe(containerEl, {
                                attributes: true,
                                attributeFilter: ['style', 'class'],
                                childList: false,
                                subtree: false
                            });
                            
                            // Store observer on element for cleanup if needed
                            $select2Container.data('widthObserver', observer);
                        }
                    }, 10);
                    
                    // Prevent width changes on all Select2 events - use a more aggressive approach
                    $select.on('select2:select select2:unselect select2:open select2:close select2:selecting', function() {
                        var $select2Container = $(this).next('.select2-container');
                        if ($select2Container.length) {
                            setTimeout(function() {
                                var containerEl = $select2Container[0];
                                containerEl.style.setProperty('width', '250px', 'important');
                                containerEl.style.setProperty('min-width', '250px', 'important');
                                containerEl.style.setProperty('max-width', '250px', 'important');
                            }, 0);
                        }
                    });

                    // Adjust Select2 border radius to match input-group styling
                    $select.on('select2:open', function() {
                        $(this).closest('.input-group').find('.select2-container--bootstrap-5 .select2-selection').css({
                            'border-top-right-radius': '0',
                            'border-bottom-right-radius': '0',
                            'border-right': 'none'
                        });
                    });
                }
            });
        },

        BindItemSelection: function () {
            var self = this;
            // Use event delegation for Select2 change events
            $("#invoiceItems").on("select2:select", ".orderItemSelect", function (e) {
                var $select = $(this);
                var $row = $select.closest("tr");
                var selected = $select.find("option:selected");
                var price = parseFloat(selected.data("price")) || 0;
                var name = selected.text();
                var selectedValue = selected.val();
                var noteRequired = selected.data("note-required") === true || selected.data("note-required") === "true";

                $row.find(".itemId").val(selectedValue);

                if (selectedValue && selectedValue > 0) {
                    $row.find(".description").val(name);
                    
                    // Update note field requirement indicator
                    var $noteField = $row.find(".note");
                    if (noteRequired) {
                        $noteField.attr("required", "required");
                        $noteField.attr("placeholder", "Note (Required)");
                    } else {
                        $noteField.removeAttr("required");
                        $noteField.attr("placeholder", "Note");
                    }
                }
                else {
                    $row.find(".description").val('');
                    $row.find(".note").removeAttr("required");
                    $row.find(".note").attr("placeholder", "Note");
                }
                $row.find(".itemPrice").val(price.toFixed(2));

                self.UpdateRowTotal($row);
            });

            // Also handle regular change event as fallback
            $("#invoiceItems").on("change", ".orderItemSelect", function () {
                var $select = $(this);
                var $row = $select.closest("tr");
                var selected = $select.find("option:selected");
                var price = parseFloat(selected.data("price")) || 0;
                var name = selected.text();
                var selectedValue = selected.val();
                var noteRequired = selected.data("note-required") === true || selected.data("note-required") === "true";

                $row.find(".itemId").val(selectedValue);

                if (selectedValue && selectedValue > 0) {
                    $row.find(".description").val(name);
                    
                    // Update note field requirement indicator
                    var $noteField = $row.find(".note");
                    if (noteRequired) {
                        $noteField.attr("required", "required");
                        $noteField.attr("placeholder", "Note (Required)");
                    } else {
                        $noteField.removeAttr("required");
                        $noteField.attr("placeholder", "Note");
                    }
                }
                else {
                    $row.find(".description").val('');
                    $row.find(".note").removeAttr("required");
                    $row.find(".note").attr("placeholder", "Note");
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

        CalculateNights: function (row) {
            var self = this;
            const checkIn = row.find('.checkIn').val();
            const checkOut = row.find('.checkOut').val();

            if (!checkIn || !checkOut) {
                row.find('.orderQty').val(0);
                return;
            }

            const d1 = new Date(checkIn);
            const d2 = new Date(checkOut);

            // Calculate difference in milliseconds
            const diffMs = d2 - d1;

            // Convert to nights
            let nights = diffMs / (1000 * 60 * 60 * 24);

            // Prevent negative or NaN
            if (isNaN(nights) || nights < 0) nights = 0;

            // Update Quantity field
            row.find('.orderQty').val(nights);

            self.UpdateRowTotal(row);
        },

        Save: function () {
            var self = this;

            // Prevent multiple saves
            if (self._isSaving) {
                console.log("Save already in progress, ignoring click");
                return;
            }
            self._isSaving = true;
            if (!self.ValidateInvoice()) {
                self._isSaving = false;
                return;
            }

            var grossAmount = self.ParseNumber($("#grossAmount").html()) || 0;
            var alreadyPaid = self._invoice.paid;

            var cash = self.ParseNumber($("#txtCash").val()) || 0;
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
                CurrencyRate: self.ParseNumber($("#txtCurrencyRate").val()) || 1,
                Status: $("#Status").val(),
                ReferenceNo: $("#ReferenceNo").val(),
                CustomerId: $("#CustomerId").val(),
                Note: $("#Note").val(),
                //CurySubTotal: self.ParseNumber($("#curySubTotal").html()) || 0,
                //SubTotal: self.ParseNumber($("#subTotal").html()) || 0,
                CurySubTotal: self.curySubTotal,
                SubTotal: self.subTotal,
                ServiceCharge: self.ParseNumber($("#serviceCharge").html()) || 0,
                GrossAmount: grossAmount,
                Paid: self.ParseNumber($("#txtPayment").val()),
                Cash: cash,
                Balance: balance,
                Change: change,
                PaymentType: self.ParseNumber($("#PaymentType").val()) || 1,
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
                    Quantity: self.ParseNumber($row.find(".orderQty").val()) || 0,
                    UnitPrice: self.ParseNumber($row.find(".itemPrice").val()) || 0,
                    Amount: self.ParseNumber($row.find(".itemTotal").val()) || 0
                });
            });

            var url = "/api/InvoicesApi/Save";                        

            // Call the API
            $.ajax({
                url: url,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(invoice),
                success: function (res) {
                    self._isSaving = false;

                    if (res.success) {
                        showToastSuccess("Invoice Save Complete");

                        self._invoice = res.invoice;
                        self.LoadInvoice();

                        if (self._mode === "Insert") {
                            history.pushState(null, "", "/Internal/Invoices/Edit/" + res.invoice.invoiceNo);
                            //window.location.href = "/Internal/Invoices/Edit/" + res.invoice.invoiceNo;
                        }
                    }
                },
                error: function (err) {
                    self._isSaving = false;
                    console.error(err);
                    showToastError("Error creating invoice. Check console for details.");
                }
            });
        },

        ValidateInvoice: function () {
            var self = this;

            let isValid = true;
            let errors = [];

            // Validate all invoice detail rows
            $("#invoiceItems tbody tr").each(function (index) {
                const $row = $(this);
                const itemId = $row.find(".orderItemSelect").val();
                const $selectedOption = $row.find(".orderItemSelect option:selected");
                const noteRequired = $selectedOption.data("note-required") === true || $selectedOption.data("note-required") === "true";
                const note = $row.find(".note").val();

                // Only validate Qty, Price, and Amount if an item is selected
                if (itemId && itemId > 0) {
                    // Validate Qty field
                    const qtyValue = $row.find(".orderQty").val();
                    if (!qtyValue || qtyValue.trim() === '' || isNaN(qtyValue)) {
                        $row.find(".orderQty").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Quantity is required.`);
                        isValid = false;
                        return false;
                    }
                    const qty = parseFloat(qtyValue);
                    if (qty <= 0) {
                        $row.find(".orderQty").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Quantity must be greater than zero.`);
                        isValid = false;
                        return false;
                    } else {
                        $row.find(".orderQty").removeClass("is-invalid");
                    }

                    // Validate Price field
                    const priceValue = $row.find(".itemPrice").val();
                    if (!priceValue || priceValue.trim() === '' || isNaN(priceValue)) {
                        $row.find(".itemPrice").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Price is required.`);
                        isValid = false;
                        return false;
                    }
                    const price = parseFloat(priceValue);
                    if (price <= 0) {
                        $row.find(".itemPrice").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Price must be greater than zero.`);
                        isValid = false;
                        return false;
                    } else {
                        $row.find(".itemPrice").removeClass("is-invalid");
                    }

                    // Validate Amount field
                    const amountValue = $row.find(".itemTotal").val();
                    if (!amountValue || amountValue.trim() === '' || isNaN(amountValue)) {
                        $row.find(".itemTotal").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Amount is required.`);
                        isValid = false;
                        return false;
                    }
                    const amount = parseFloat(amountValue);
                    if (amount <= 0) {
                        $row.find(".itemTotal").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Amount must be greater than zero.`);
                        isValid = false;
                        return false;
                    } else {
                        $row.find(".itemTotal").removeClass("is-invalid");
                    }

                    // Validate calculated amount matches Qty * Price
                    const calculatedAmount = qty * price;
                    const amountDifference = Math.abs(amount - calculatedAmount);
                    if (amountDifference > 0.01) { // Allow small floating point differences
                        $row.find(".itemTotal").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Amount (${amount.toFixed(2)}) does not match Quantity × Price (${calculatedAmount.toFixed(2)}).`);
                        isValid = false;
                        return false;
                    }

                    // Validate note required field
                    if (noteRequired && (!note || note.trim().length === 0)) {
                        $row.find(".note").addClass("is-invalid");
                        showToastError(`Row ${index + 1}: Note is required for selected item.`);
                        isValid = false;
                        return false;
                    } else {
                        $row.find(".note").removeClass("is-invalid");
                    }
                }

            //if (!$("#ReferenceNo").val()) {
            //    alert("Reference No is required.");
            //    return;
            //}
            if (!$("#CustomerId").val()) {
                $("#CustomerId").addClass("is-invalid");
                showToastError("Customer is required.");
                isValid = false;
                return false;
            }
            else {
                $("#CustomerId").removeClass("is-invalid");
            }

            var grossAmount = self.ParseNumber($("#grossAmount").html());
            if (grossAmount <= 0) {
                showToastError("Gross amount must be greater than zero.");
                isValid = false;
                return false;
            }
            //if ($("#invoiceDetailsTable tbody tr").length === 0) {
            //    alert("Please add at least one invoice detail.");
            //   isValid = false;
            //}

            var paidAmount = self.ParseNumber($("#txtPayment").val());
            var balanceAmount = self.ParseNumber($("#txtBalance").html());

            if ($("#InvoiceNo").val() == 0) {
                balanceAmount = grossAmount;
            }

            if (paidAmount > balanceAmount) {
                if ($("#InvoiceNo").val() == 0) {
                    showToastError("Paid amount must be less than or equals to Gross Amount");
                } else {
                    showToastError("Paid amount must be less than or equals to Balance Amount");
                }

                isValid = false;
                return false;
            }

           
                
                // Validate Stay type specific fields
                if (self._type == 3) {
                    const checkIn = $row.find(".checkIn").val();
                    const checkOut = $row.find(".checkOut").val();

                    if (!itemId) {
                        $row.find(".orderItemSelect").addClass("is-invalid");
                        showToastError('Invalid item selected');
                        isValid = false;
                        return false;
                    }
                    else {
                        $row.find(".orderItemSelect").removeClass("is-invalid");
                    }

                    if (!checkIn || !checkOut) {
                        $row.find(".checkIn, .checkOut").addClass("is-invalid");
                        showToastError('Invalid check-in & check-out dates');
                        isValid = false;
                        return false;
                    }
                    else {
                        $row.find(".checkIn, .checkOut").removeClass("is-invalid");
                    }

                    if (checkIn && checkOut) {
                        const inDate = new Date(checkIn);
                        const outDate = new Date(checkOut);

                        if (inDate > outDate) {
                            isValid = false;
                            errors.push(`Row ${index + 1}: Check-In cannot be after Check-Out.`);
                            $row.find(".checkIn, .checkOut").addClass("is-invalid");
                            showToastError('Invalid check-in & check-out dates');
                        } else {
                            $row.find(".checkIn, .checkOut").removeClass("is-invalid");
                        }
                    }
                }
            });

            // Validate payment reference when method requires it
            var method = parseInt($("#PaymentType").val());
            if (method === 2 || method === 3) {
                var pref = $("#txtPaymentReference").val();
                if (!pref || pref.trim().length === 0) {
                    showToastError("Payment Reference is required for selected method.");
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
            var curySubTotal = 0;

            const selectedCurrency = $("#ddlCurrency").val();

            $("#invoiceItems tbody tr").each(function () {
                curySubTotal += parseFloat($(this).find(".itemTotal").val()) || 0;
            });

            self.curySubTotal = curySubTotal;
            $("#curySubTotal").text(self._formatter.format(curySubTotal));
            $("#curySubTotal_code").text(selectedCurrency);

            self.CalculateGrossTotal();
        },

        CalculateGrossTotal: function () {
            var self = this;

            //var curySubTotal = self.ParseNumber($("#curySubTotal").html());
            const selectedCurrency = $("#ddlCurrency").val() || "LKR";
            const currencyRate = self.ParseNumber($("#txtCurrencyRate").val() || 1);
            self.subTotal = self.ConvertCurrency(self.curySubTotal, selectedCurrency, self._baseCurrency, currencyRate);

            var serviceCharge = 0;
            //Service charges applyes to Dining only
            if ($("#Type").val() == 1) {
                serviceCharge = self.subTotal * self.serviceCharge;
            }

            var grossTotal = parseFloat(self.subTotal) + parseFloat(serviceCharge);

            $("#subTotal").html(self._formatter.format(self.subTotal));
            $("#serviceCharge").html(self._formatter.format(serviceCharge));
            $("#grossAmount").html(self._formatter.format(grossTotal));
        },

        CalculateBalanceDue: function () {
            var self = this;

            var grossAmount = self.ParseNumber($("#grossAmount").html());
            var balance = self.ParseNumber($("#txtBalance").html());
            var cash = self.ParseNumber($("#txtCash").val());
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

            $("#txtPayment").val(payment);// this is hidden text
            $("#txtBalanceDue").html(self._formatter.format(balanceDue));
        },

        ParseNumber: function (value) {
            if (value === null || value === undefined) return 0;
            // convert to string, remove anything except digits, minus sign and dot
            var s = String(value).trim();
            // remove common currency symbols and non-number chars except dot and minus
            s = s.replace(/[^0-9.\-]/g, "");
            // if more than one dot (rare), keep first dot only
            var firstDot = s.indexOf('.');
            if (firstDot !== -1) {
                s = s.substring(0, firstDot + 1) + s.substring(firstDot + 1).replace(/\./g, '');
            }
            var n = parseFloat(s);
            return isNaN(n) ? 0 : n;
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

// Quick Customer Form Handler
function InitializeQuickCustomerForm() {
    $('#frmQuickCustomer').on('submit', function (e) {
        e.preventDefault();
        const $form = $(this);
        const formData = $form.serialize();
        const token = $form.find('input[name="__RequestVerificationToken"]').val();
        $('#quickCustomerErrors').addClass('d-none').empty();

        $.ajax({
            url: '/Customers/CreateQuick',
            type: 'POST',
            data: formData,
            headers: { 'RequestVerificationToken': token },
            success: function (res) {
                if (res && res.success) {
                    const id = res.customer.id;
                    const roomNo = res.customer.roomNo || '';
                    const name = res.customer.firstName + ' ' + res.customer.lastName;
                    const displayText = roomNo ? '#' + roomNo + ' - ' + name : name;
                    const $ddl = $('#CustomerId');

                    if ($ddl.find('option[value="' + id + '"]').length === 0 && res.customer.active) {
                        $ddl.append($('<option>', { value: id, text: displayText }));
                    }

                    $ddl.val(id).trigger('change');
                    const modalEl = document.getElementById('quickCustomerModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    modal.hide();
                    $form[0].reset();

                    $('#Email').prop('disabled', false);
                    $('#qcCustomerId').val('');
                    ResetQuickCustomer();
                } else if (res && res.errors) {
                    const $err = $('#quickCustomerErrors');
                    $err.removeClass('d-none');
                    res.errors.forEach(function (m) { $err.append($('<div>').text(m)); });
                }
            },
            error: function (xhr) {
                const $err = $('#quickCustomerErrors');
                $err.removeClass('d-none').text('Failed to create customer. Please try again.');
            }
        });
    });

    // Load existing by email and populate
    $('#Email').on('blur', function () {
        const email = $(this).val();
        if (!email) return;
        $.getJSON('/Customers/GetByEmail', { email: email }, function (res) {
            if (res && res.success && res.exists) {
                const c = res.customer;
                $('#qcCustomerId').val(c.id);
                $('input[name="FirstName"]').val(c.firstName);
                $('input[name="LastName"]').val(c.lastName);
                $('input[name="ContactNo"]').val(c.contactNo);
                $('input[name="Address"]').val(c.address);
                $('select[name="Country"]').val(c.country);
                $('input[name="PassportNo"]').val(c.passportNo);
                $('input[name="RoomNo"]').val(c.roomNo || '');
                $('input[name="Active"][value="' + (c.active ? 'true' : 'false') + '"]').prop('checked', true);
                $('#Email').prop('readonly', true);
            }
        });
    });

    $('#frmQuickCustomerClose').on('click', function () {
        ResetQuickCustomer();
    });
}

function ResetQuickCustomer() {
    $('#qcCustomerId').val(0);
    $('input[name="Email"]').val('');
    $('input[name="FirstName"]').val('');
    $('input[name="LastName"]').val('');
    $('input[name="ContactNo"]').val('');
    $('input[name="Address"]').val('');
    $('select[name="Country"]').val(0);
    $('input[name="PassportNo"]').val('');
    $('input[name="RoomNo"]').val('');
    $('input[name="Active"][value="true"]').prop('checked', true);
    $('#Email').prop('readonly', false);
    $('#quickCustomerErrors').addClass('d-none').empty();
}


// Initialize invoice page
function InitializeInvoicePage(invoiceData, mode, isAdmin, currencyData) {
    $(document).ready(function () {
        const obj = {
            mode: mode,
            invoice: invoiceData,
            isAdmin: isAdmin || false,
            currencyData: currencyData || []
        };
        $().invoice(obj);
        
        // Initialize quick customer form
        InitializeQuickCustomerForm();
    });
}
