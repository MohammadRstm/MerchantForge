using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAttributeConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedValues",
                table: "product_attribute_definitions",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "product_attribute_definitions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000001"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { "[\"Black\",\"White\",\"Red\",\"Blue\",\"Green\"]", true });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000002"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { "[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"XXL\"]", true });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000003"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000004"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000005"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000006"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000007"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000008"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-000000000009"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-00000000000a"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000001"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000002"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000003"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000004"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000005"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000006"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000007"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000008"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-000000000009"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a2000000-0000-4000-8000-00000000000a"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000001"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000002"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000003"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000004"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000005"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000006"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000007"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000008"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-000000000009"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "product_attribute_definitions",
                keyColumn: "Id",
                keyValue: new Guid("a3000000-0000-4000-8000-00000000000a"),
                columns: new[] { "AllowedValues", "IsRequired" },
                values: new object[] { null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedValues",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "product_attribute_definitions");
        }
    }
}
