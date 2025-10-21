using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class Payment_FK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceNo",
                table: "Payments",
                column: "InvoiceNo");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceNo",
                table: "Payments",
                column: "InvoiceNo",
                principalTable: "Invoices",
                principalColumn: "InvoiceNo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceNo",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InvoiceNo",
                table: "Payments");
        }
    }
}
