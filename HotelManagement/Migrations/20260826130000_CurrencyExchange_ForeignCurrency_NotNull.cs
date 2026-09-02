using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    [DbContext(typeof(Data.HotelContext))]
    [Migration("20260826130000_CurrencyExchange_ForeignCurrency_NotNull")]
    public partial class CurrencyExchange_ForeignCurrency_NotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE CurrencyExchanges SET ForeignCurrency = '' WHERE ForeignCurrency IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "ForeignCurrency",
                table: "CurrencyExchanges",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ForeignCurrency",
                table: "CurrencyExchanges",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true,
                defaultValue: null);
        }
    }
}
