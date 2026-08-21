using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedProductMerchandisingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompareAtPrice",
                table: "products",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndsAt",
                table: "products",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "products",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "products",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_products_BusinessId_Sku",
                table: "products",
                columns: new[] { "BusinessId", "Sku" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_CompareAtPrice_GreaterThanPrice",
                table: "products",
                sql: "`CompareAtPrice` IS NULL OR `CompareAtPrice` > `Price`");

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_StockQuantity_NonNegative",
                table: "products",
                sql: "`StockQuantity` IS NULL OR `StockQuantity` >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_BusinessId_Sku",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_products_CompareAtPrice_GreaterThanPrice",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_products_StockQuantity_NonNegative",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CompareAtPrice",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SaleEndsAt",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "products");
        }
    }
}
