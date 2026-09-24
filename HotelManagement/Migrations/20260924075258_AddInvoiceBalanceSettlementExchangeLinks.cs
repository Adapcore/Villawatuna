using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBalanceSettlementExchangeLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExchangeId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "CurrencyExchanges",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ExchangeId",
                table: "Invoices",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_InvoiceId",
                table: "CurrencyExchanges",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchanges_Invoices_InvoiceId",
                table: "CurrencyExchanges",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "InvoiceNo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CurrencyExchanges_ExchangeId",
                table: "Invoices",
                column: "ExchangeId",
                principalTable: "CurrencyExchanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyExchanges_Invoices_InvoiceId",
                table: "CurrencyExchanges");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CurrencyExchanges_ExchangeId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ExchangeId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyExchanges_InvoiceId",
                table: "CurrencyExchanges");

            migrationBuilder.DropColumn(
                name: "ExchangeId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "CurrencyExchanges");
        }
    }
}
