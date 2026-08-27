using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddGroceryDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "business_domains",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[] { new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Grocery", "grocery", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "BusinessDomainId", "BusinessId", "CreatedAt", "DisplayOrder", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("c4000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Vegetables", "vegetables", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c4000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Fruits", "fruits", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c4000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Dairy", "dairy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c4000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Bakery", "bakery", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c4000000-0000-4000-8000-000000000005"), new Guid("d1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Beverages", "beverages", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "product_attribute_definitions",
                columns: new[] { "Id", "AllowedValues", "BusinessDomainId", "CreatedAt", "DisplayOrder", "IsActive", "IsRequired", "Key", "Label", "UpdatedAt", "ValueType" },
                values: new object[,]
                {
                    { new Guid("a4000000-0000-4000-8000-000000000001"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, true, "unit", "Unit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-000000000002"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, false, "organic", "Organic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" },
                    { new Guid("a4000000-0000-4000-8000-000000000003"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, false, "origin", "Origin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-000000000004"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, false, "brand", "Brand", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-000000000005"), "[\"Vegan\",\"Vegetarian\",\"Gluten-Free\",\"Dairy-Free\",\"Nut-Free\",\"Sugar-Free\"]", new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, false, "dietaryTags", "Dietary tags", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TextList" },
                    { new Guid("a4000000-0000-4000-8000-000000000006"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, false, "storageInstructions", "Storage instructions", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-000000000007"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, false, "shelfLifeDays", "Shelf life (days)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number" },
                    { new Guid("a4000000-0000-4000-8000-000000000008"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, false, "packSize", "Pack size", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-000000000009"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, false, "ingredients", "Ingredients", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" },
                    { new Guid("a4000000-0000-4000-8000-00000000000a"), null, new Guid("d1000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, false, "isFrozen", "Frozen", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c4000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c4000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c4000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c4000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c4000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a4000000-0000-4000-8000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "business_domains",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000004"));
        }
    }
}
