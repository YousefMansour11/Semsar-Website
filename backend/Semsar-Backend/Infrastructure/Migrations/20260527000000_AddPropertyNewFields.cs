using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryText",
                table: "UnitVariants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteCount",
                table: "UnitVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InquiryCount",
                table: "UnitVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "UnitVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "UnitVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "UnitVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Units",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConstructionStatus",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryText",
                table: "Units",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteCount",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HighlightsAr",
                table: "Units",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InquiryCount",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NearbyPlaces",
                table: "Units",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearbyPlacesAr",
                table: "Units",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipType",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VirtualTourUrl",
                table: "Units",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Properties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConstructionStatus",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryText",
                table: "Properties",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteCount",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HighlightsAr",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InquiryCount",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "Properties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NearbyPlaces",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearbyPlacesAr",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipType",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VirtualTourUrl",
                table: "Properties",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConstructionStatus",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryText",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteCount",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InquiryCount",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VirtualTourUrl",
                table: "Projects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryText",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "InquiryCount",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "UnitVariants");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ConstructionStatus",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DeliveryText",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "HighlightsAr",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "InquiryCount",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "NearbyPlaces",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "NearbyPlacesAr",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "VirtualTourUrl",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ConstructionStatus",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "DeliveryText",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "HighlightsAr",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "InquiryCount",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NearbyPlaces",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NearbyPlacesAr",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "VirtualTourUrl",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ConstructionStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeliveryText",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InquiryCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "VirtualTourUrl",
                table: "Projects");
        }
    }
}
