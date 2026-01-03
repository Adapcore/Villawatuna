using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCurryAmountColumnsToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurryGrossAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurryLastPaid",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurryBalance",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurryChange",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurryGrossAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurryLastPaid",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurryBalance",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurryChange",
                table: "Invoices");
        }
    }
}

