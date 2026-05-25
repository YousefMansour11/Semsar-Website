using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeAndUnitMinMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Locations_LocationId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Locations_LocationId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_Bedrooms",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_CreatedAt",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_IsDeleted",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_Price",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_ProjectId_Code",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_PropertyType",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_Slug",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Properties_Bedrooms",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_CreatedAt",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_IsDeleted",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_IsFeatured",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ListingType_PropertyType_Price_Location_IsFeatured",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_Price",
                table: "Properties");

            migrationBuilder.AddColumn<double>(
                name: "MaxArea",
                table: "Units",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "Units",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinArea",
                table: "Units",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                table: "Units",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("UPDATE Units SET MinPrice = Price, MaxPrice = NULL, MinArea = Size, MaxArea = NULL WHERE Price IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Units");

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "UnitInstallmentPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "UnitInstallmentPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Installment");

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "PropertyInstallmentPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "PropertyInstallmentPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Installment");

            migrationBuilder.CreateIndex(
                name: "IX_Units_ProjectId",
                table: "Units",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Locations_LocationId",
                table: "Properties",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Locations_LocationId",
                table: "Units",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Locations_LocationId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Locations_LocationId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_ProjectId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MaxArea",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MinArea",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "UnitInstallmentPlans");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "UnitInstallmentPlans");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PropertyInstallmentPlans");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "PropertyInstallmentPlans");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Units",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Size",
                table: "Units",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Bedrooms",
                table: "Units",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_Units_CreatedAt",
                table: "Units",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Units_IsDeleted",
                table: "Units",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Price",
                table: "Units",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Units_ProjectId_Code",
                table: "Units",
                columns: new[] { "ProjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_PropertyType",
                table: "Units",
                column: "PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Slug",
                table: "Units",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Bedrooms",
                table: "Properties",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_CreatedAt",
                table: "Properties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_IsDeleted",
                table: "Properties",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_IsFeatured",
                table: "Properties",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ListingType_PropertyType_Price_Location_IsFeatured",
                table: "Properties",
                columns: new[] { "ListingType", "PropertyType", "Price", "Location", "IsFeatured" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Price",
                table: "Properties",
                column: "Price");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Locations_LocationId",
                table: "Properties",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Locations_LocationId",
                table: "Units",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
