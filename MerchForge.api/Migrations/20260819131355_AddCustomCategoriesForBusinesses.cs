using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCategoriesForBusinesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "categories",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-4000-8000-000000000001"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-4000-8000-000000000002"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-4000-8000-000000000003"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c2000000-0000-4000-8000-000000000001"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c2000000-0000-4000-8000-000000000002"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c2000000-0000-4000-8000-000000000003"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c3000000-0000-4000-8000-000000000001"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c3000000-0000-4000-8000-000000000002"),
                column: "BusinessId",
                value: null);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("c3000000-0000-4000-8000-000000000003"),
                column: "BusinessId",
                value: null);

            // Created before dropping the old index below: MariaDB refuses to drop an
            // index that's currently the only one covering an FK column
            // (BusinessDomainId, here, for FK_categories_business_domains_BusinessDomainId).
            // This new index's leftmost column is also BusinessDomainId, so it can
            // take over that role with no gap where the FK is left uncovered.
            migrationBuilder.CreateIndex(
                name: "IX_categories_BusinessDomainId_BusinessId_Slug",
                table: "categories",
                columns: new[] { "BusinessDomainId", "BusinessId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_BusinessId",
                table: "categories",
                column: "BusinessId");

            migrationBuilder.DropIndex(
                name: "IX_categories_BusinessDomainId_Slug",
                table: "categories");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_businesses_BusinessId",
                table: "categories",
                column: "BusinessId",
                principalTable: "businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_businesses_BusinessId",
                table: "categories");

            // Same reordering as Up(), mirrored: create the index that will cover
            // the BusinessDomainId FK before dropping the one currently covering it.
            migrationBuilder.CreateIndex(
                name: "IX_categories_BusinessDomainId_Slug",
                table: "categories",
                columns: new[] { "BusinessDomainId", "Slug" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_categories_BusinessDomainId_BusinessId_Slug",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_BusinessId",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "categories");
        }
    }
}
