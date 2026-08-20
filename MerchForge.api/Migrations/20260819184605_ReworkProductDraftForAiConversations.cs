using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class ReworkProductDraftForAiConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalImagePath",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "PendingDetails",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "ProcessedImagePath",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "StructuredData",
                table: "product_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "product_drafts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "product_drafts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ConversationId",
                table: "product_drafts",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // No defaultValue. EF scaffolds Guid.Empty, but that is not a user that
            // can ever exist, so leaving it as a column default would be misleading.
            // product_drafts was verified empty before this migration was written -
            // the dropped columns below belonged to an earlier draft shape that never
            // held data - so no backfill value is needed.
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "product_drafts",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Draft",
                table: "product_drafts",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ImageModificationPrompt",
                table: "product_drafts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Messages",
                table: "product_drafts",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginalImageUrl",
                table: "product_drafts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedImageUrl",
                table: "product_drafts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "product_drafts",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_product_drafts_BusinessId_Status",
                table: "product_drafts",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_drafts_ProductId",
                table: "product_drafts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_product_drafts_products_ProductId",
                table: "product_drafts",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_drafts_products_ProductId",
                table: "product_drafts");

            migrationBuilder.DropIndex(
                name: "IX_product_drafts_BusinessId_Status",
                table: "product_drafts");

            migrationBuilder.DropIndex(
                name: "IX_product_drafts_ProductId",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "Draft",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "ImageModificationPrompt",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "Messages",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "OriginalImageUrl",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "ProcessedImageUrl",
                table: "product_drafts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "product_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "product_drafts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "product_drafts",
                keyColumn: "Provider",
                keyValue: null,
                column: "Provider",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "product_drafts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "product_drafts",
                keyColumn: "ConversationId",
                keyValue: null,
                column: "ConversationId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ConversationId",
                table: "product_drafts",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginalImagePath",
                table: "product_drafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PendingDetails",
                table: "product_drafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedImagePath",
                table: "product_drafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StructuredData",
                table: "product_drafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
