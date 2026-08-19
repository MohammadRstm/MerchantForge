using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAttributeDefinitionsAndMetadataShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "meta_data_shape",
                table: "businesses",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "product_attribute_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessDomainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attribute_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_attribute_definitions_business_domains_BusinessDomai~",
                        column: x => x.BusinessDomainId,
                        principalTable: "business_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "product_attribute_definitions",
                columns: new[] { "Id", "BusinessDomainId", "CreatedAt", "DisplayOrder", "IsActive", "Key", "Label", "UpdatedAt", "ValueType" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "colors", "Colors", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "sizes", "Sizes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "material", "Material", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "fit", "Fit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "pattern", "Pattern", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "gender", "Gender", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "season", "Season", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000008"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "careInstructions", "Care instructions", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-000000000009"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, "countryOfOrigin", "Country of origin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a1000000-0000-4000-8000-00000000000a"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, "handmade", "Handmade", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a2000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "ingredients", "Ingredients", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a2000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "allergens", "Allergens", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a2000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "spicy", "Spicy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a2000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "vegetarian", "Vegetarian", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a2000000-0000-4000-8000-000000000005"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "vegan", "Vegan", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a2000000-0000-4000-8000-000000000006"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "glutenFree", "Gluten free", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a2000000-0000-4000-8000-000000000007"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "calories", "Calories", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number" },
                    { new Guid("a2000000-0000-4000-8000-000000000008"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "portionSize", "Portion size", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a2000000-0000-4000-8000-000000000009"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, "preparationMinutes", "Preparation time (minutes)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number" },
                    { new Guid("a2000000-0000-4000-8000-00000000000a"), new Guid("d1000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, "servingTemperature", "Serving temperature", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "brand", "Brand", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "model", "Model", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "storage", "Storage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "ram", "RAM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000005"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "screenSize", "Screen size", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000006"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "batteryCapacity", "Battery capacity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-000000000007"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "colors", "Colors", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a3000000-0000-4000-8000-000000000008"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "connectivity", "Connectivity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a3000000-0000-4000-8000-000000000009"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, "operatingSystem", "Operating system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a3000000-0000-4000-8000-00000000000a"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, "warrantyMonths", "Warranty (months)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_definitions_BusinessDomainId_Key",
                table: "product_attribute_definitions",
                columns: new[] { "BusinessDomainId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "meta_data_shape",
                table: "businesses");
        }
    }
}
