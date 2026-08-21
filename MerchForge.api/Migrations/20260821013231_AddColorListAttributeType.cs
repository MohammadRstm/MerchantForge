using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddColorListAttributeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000001"),
                columns: new[] { "AllowedValues", "ValueType" },
                values: new object[] { null, "ColorList" });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000007"),
                column: "ValueType",
                value: "ColorList");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000001"),
                columns: new[] { "AllowedValues", "ValueType" },
                values: new object[] { "[\"Black\",\"White\",\"Red\",\"Blue\",\"Green\"]", "TextList" });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000007"),
                column: "ValueType",
                value: "TextList");
        }
    }
}
