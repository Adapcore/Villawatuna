using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class Invoice_removeOrderFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Orders_OrderNo",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OrderNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OrderNo",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "InvoiceDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "InvoiceDetails");

            migrationBuilder.AddColumn<int>(
                name: "OrderNo",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderNo",
                table: "Invoices",
                column: "OrderNo");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Orders_OrderNo",
                table: "Invoices",
                column: "OrderNo",
                principalTable: "Orders",
                principalColumn: "OrderNo",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
