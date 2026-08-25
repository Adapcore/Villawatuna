using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    [DbContext(typeof(Data.HotelContext))]
    [Migration("20260824160000_CurrencyExchange_AddInvoiceAndExpenseFKs")]
    public partial class CurrencyExchange_AddInvoiceAndExpenseFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceNo",
                table: "CurrencyExchanges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpenseId",
                table: "CurrencyExchanges",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_InvoiceNo",
                table: "CurrencyExchanges",
                column: "InvoiceNo");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_ExpenseId",
                table: "CurrencyExchanges",
                column: "ExpenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchanges_Invoices_InvoiceNo",
                table: "CurrencyExchanges",
                column: "InvoiceNo",
                principalTable: "Invoices",
                principalColumn: "InvoiceNo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchanges_Expenses_ExpenseId",
                table: "CurrencyExchanges",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyExchanges_Invoices_InvoiceNo",
                table: "CurrencyExchanges");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyExchanges_Expenses_ExpenseId",
                table: "CurrencyExchanges");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyExchanges_InvoiceNo",
                table: "CurrencyExchanges");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyExchanges_ExpenseId",
                table: "CurrencyExchanges");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "CurrencyExchanges");

            migrationBuilder.DropColumn(
                name: "ExpenseId",
                table: "CurrencyExchanges");
        }
    }
}
