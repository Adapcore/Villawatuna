using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Migrations
{
    /// <inheritdoc />
    public partial class Customer_addRoomNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomNo",
                table: "Customers",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoomNo",
                table: "Customers");
        }
    }
}
