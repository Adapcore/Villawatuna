using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class PettyCashBook_AddRefillColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MoneyInHand",
                table: "PettyCashBook",
                newName: "PettyCashRefillOpening");

            migrationBuilder.AddColumn<decimal>(
                name: "PettyCashRefillClosing",
                table: "PettyCashBook",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PettyCashRefillClosing",
                table: "PettyCashBook");

            migrationBuilder.RenameColumn(
                name: "PettyCashRefillOpening",
                table: "PettyCashBook",
                newName: "MoneyInHand");
        }
    }
}
