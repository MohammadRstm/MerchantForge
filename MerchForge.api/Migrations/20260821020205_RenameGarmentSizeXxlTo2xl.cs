using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class RenameGarmentSizeXxlTo2xl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000002"),
                column: "AllowedValues",
                value: "[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"2XL\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000002"),
                column: "AllowedValues",
                value: "[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"XXL\"]");
        }
    }
}
