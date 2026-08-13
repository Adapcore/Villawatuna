using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashBookTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PettyCashBook",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CloseBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalIncomeBaseCurry = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExpences = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HandedOver = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoneyInHand = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    Expences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Income = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalIncome = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashBook", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashBook_Date",
                table: "PettyCashBook",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashBook");
        }
    }
}
