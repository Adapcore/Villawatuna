using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    [DbContext(typeof(Data.HotelContext))]
    [Migration("20260826120000_CurrencyExchange_RemoveForeignCurrencyId")]
    public partial class CurrencyExchange_RemoveForeignCurrencyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForeignCurrencyId",
                table: "CurrencyExchanges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ForeignCurrencyId",
                table: "CurrencyExchanges",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
