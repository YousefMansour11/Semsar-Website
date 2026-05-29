using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<double>(type: "float", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RentPerMonth = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: false),
                    View = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasBalcony = table.Column<bool>(type: "bit", nullable: false),
                    HasParking = table.Column<bool>(type: "bit", nullable: false),
                    FloorPlanUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvailabilityStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitVariants_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitVariants_IsDeleted",
                table: "UnitVariants",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnitVariants_PublicId",
                table: "UnitVariants",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitVariants_PublicKey",
                table: "UnitVariants",
                column: "PublicKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitVariants_UnitId",
                table: "UnitVariants",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitVariants");
        }
    }
}
