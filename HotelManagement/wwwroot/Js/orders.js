// Order Create page functionality
function InitializeOrderCreate() {
    $(document).ready(function () {
        let itemIndex = 0;

        function recalcTotals() {
            let subtotal = 0;
            let isFree = $("#IsFreeOfCharge").is(":checked");
            let isDining = $("#Dining").is(":checked");

            $("#orderItemsTable tbody tr").each(function () {
                let qty = parseFloat($(this).find("input[name*='Qty']").val()) || 0;
                let price = parseFloat($(this).find("input[name*='UnitPrice']").val()) || 0;
                let amount = qty * price;
                $(this).find("input[name*='Amount']").val(amount.toFixed(2));
                subtotal += amount;
            });

            if (isFree)
                subtotal = 0;

            $("#SubTotal").val(subtotal.toFixed(2));

            let service = isDining ? subtotal * 0.10 : 0;
            $("#ServiceCharge").val(service.toFixed(2));

            $("#GrossAmount").val((subtotal + service).toFixed(2));
        }

        function reindexRows() {
            $("#orderItemsTable tbody tr").each(function (i) {
                $(this).find("input").each(function () {
                    const name = $(this).attr("name");
                    const newName = name.replace(/\d+/, i);
                    $(this).attr("name", newName);
                });
            });
            itemIndex = $("#orderItemsTable tbody tr").length;
            recalcTotals();
        }

        $("#addItemBtn").click(function () {
            let row = `
            <tr>
                <td><input name="OrderItems[${itemIndex}].ItemId" class="form-control" /></td>
                <td><input name="OrderItems[${itemIndex}].Comments" class="form-control" /></td>
                <td><input name="OrderItems[${itemIndex}].Qty" class="form-control" type="number" value="1" /></td>
                <td><input name="OrderItems[${itemIndex}].UnitPrice" class="form-control" type="number" step="0.01" /></td>
                <td><input name="OrderItems[${itemIndex}].Amount" class="form-control" readonly /></td>
                <td><button type="button" class="btn btn-danger btn-sm removeItemBtn">X</button></td>
            </tr>`;
            $("#orderItemsTable tbody").append(row);
            itemIndex++;
        });

        // Bind recalculation to inputs and checkboxes
        $(document).on("input", "input[name*='Qty'], input[name*='UnitPrice']", recalcTotals);
        $(document).on("change", "#IsFreeOfCharge, #Dining", recalcTotals);

        $(document).on("click", ".removeItemBtn", function (e) {
            e.preventDefault();

            // Remove the row
            $(this).closest("tr").remove();

            // Re-index all rows to maintain model binding
            $("#orderItemsTable tbody tr").each(function (rowIndex, row) {
                $(row).find("input, select, textarea").each(function () {
                    var nameAttr = $(this).attr("name");
                    if (nameAttr) {
                        // Replace index inside [0], [1], etc.
                        var newName = nameAttr.replace(/\[\d+\]/, "[" + rowIndex + "]");
                        $(this).attr("name", newName);

                        // Update id too (to avoid duplicate IDs in DOM)
                        var idAttr = $(this).attr("id");
                        if (idAttr) {
                            var newId = idAttr.replace(/\_\d+\_/, "_" + rowIndex + "_");
                            $(this).attr("id", newId);
                        }
                    }
                });
            });

            recalcTotals();
        });
    });
}

