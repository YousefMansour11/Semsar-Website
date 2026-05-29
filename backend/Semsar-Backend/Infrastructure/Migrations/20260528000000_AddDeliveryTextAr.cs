using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTextAr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryTextAr",
                table: "UnitVariants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTextAr",
                table: "Units",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTextAr",
                table: "Properties",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTextAr",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryTextAr",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "DeliveryTextAr",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DeliveryTextAr",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "DeliveryTextAr",
                table: "Projects");
        }
    }
}
