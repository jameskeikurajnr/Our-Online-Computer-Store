using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineComputerStore.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddClickAndCollect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FulfillmentMethod",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "Ship");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfillmentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "Orders");
        }
    }
}
