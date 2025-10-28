using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class Invoice_RenamePaidCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Paid",
                table: "Invoices",
                newName: "TotalPaid");

            migrationBuilder.RenameColumn(
                name: "Cash",
                table: "Invoices",
                newName: "LastPaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPaid",
                table: "Invoices",
                newName: "Paid");

            migrationBuilder.RenameColumn(
                name: "LastPaid",
                table: "Invoices",
                newName: "Cash");
        }
    }
}
