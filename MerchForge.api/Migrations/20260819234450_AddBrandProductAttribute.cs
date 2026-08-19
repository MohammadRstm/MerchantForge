using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandProductAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "product_attribute_definitions",
                columns: new[] { "Id", "AllowedValues", "BusinessDomainId", "CreatedAt", "DisplayOrder", "IsActive", "IsRequired", "Key", "Label", "UpdatedAt", "ValueType" },
                values: new object[] { new Guid("a1000000-0000-4000-8000-00000000000b"), null, new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, true, false, "brand", "Brand", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-00000000000b"));
        }
    }
}
