using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectStartingPriceAndNearbyPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NearbyPlaces",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StartingPrice",
                table: "Projects",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitVideos_IsDeleted",
                table: "UnitVideos",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnitInstallmentPlans_IsDeleted",
                table: "UnitInstallmentPlans",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnitImage_IsDeleted",
                table: "UnitImage",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_IsDeleted",
                table: "Settings",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVideos_IsDeleted",
                table: "PropertyVideos",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstallmentPlans_IsDeleted",
                table: "PropertyInstallmentPlans",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_IsDeleted",
                table: "PropertyImages",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVideos_IsDeleted",
                table: "ProjectVideos",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImages_IsDeleted",
                table: "ProjectImages",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Phone",
                table: "Leads",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_LandRequests_IsDeleted",
                table: "LandRequests",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LandRequests_Phone",
                table: "LandRequests",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_IsDeleted",
                table: "Contacts",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_IsDeleted",
                table: "BookingRequests",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_Phone",
                table: "BookingRequests",
                column: "Phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitVideos_IsDeleted",
                table: "UnitVideos");

            migrationBuilder.DropIndex(
                name: "IX_UnitInstallmentPlans_IsDeleted",
                table: "UnitInstallmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_UnitImage_IsDeleted",
                table: "UnitImage");

            migrationBuilder.DropIndex(
                name: "IX_Settings_IsDeleted",
                table: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_PropertyVideos_IsDeleted",
                table: "PropertyVideos");

            migrationBuilder.DropIndex(
                name: "IX_PropertyInstallmentPlans_IsDeleted",
                table: "PropertyInstallmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PropertyImages_IsDeleted",
                table: "PropertyImages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectVideos_IsDeleted",
                table: "ProjectVideos");

            migrationBuilder.DropIndex(
                name: "IX_ProjectImages_IsDeleted",
                table: "ProjectImages");

            migrationBuilder.DropIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_Phone",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_LandRequests_IsDeleted",
                table: "LandRequests");

            migrationBuilder.DropIndex(
                name: "IX_LandRequests_Phone",
                table: "LandRequests");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_IsDeleted",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_IsDeleted",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_Phone",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "NearbyPlaces",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartingPrice",
                table: "Projects");
        }
    }
}
