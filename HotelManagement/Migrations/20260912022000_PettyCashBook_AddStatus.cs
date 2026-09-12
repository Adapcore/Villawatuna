using HotelManagement.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HotelManagement.Data.HotelContext))]
    [Migration("20260912022000_PettyCashBook_AddStatus")]
    public class PettyCashBook_AddStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PettyCashBook",
                type: "int",
                nullable: false,
                defaultValue: (int)PettyCashStatus.Open);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PettyCashBook");
        }
    }
}
