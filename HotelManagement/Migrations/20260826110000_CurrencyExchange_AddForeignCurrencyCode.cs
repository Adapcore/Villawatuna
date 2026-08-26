using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    [DbContext(typeof(Data.HotelContext))]
    [Migration("20260826110000_CurrencyExchange_AddForeignCurrencyCode")]
    public partial class CurrencyExchange_AddForeignCurrencyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ForeignCurrency",
                table: "CurrencyExchanges",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForeignCurrency",
                table: "CurrencyExchanges");
        }
    }
}
