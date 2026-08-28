using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "businesses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "businesses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BusinessHours",
                table: "businesses",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "businesses",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "businesses",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "businesses",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "businesses",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "businesses",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SocialLinks",
                table: "businesses",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "businesses",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "businesses",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteCustomizationValues",
                table: "businesses",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "businesses",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "website_template_customizable_components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WebsiteTemplateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowedValues = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HelpText = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_template_customizable_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_website_template_customizable_components_website_templates_W~",
                        column: x => x.WebsiteTemplateId,
                        principalTable: "website_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "website_template_customizable_components",
                columns: new[] { "Id", "AllowedValues", "CreatedAt", "DisplayOrder", "HelpText", "IsActive", "IsRequired", "Key", "Label", "UpdatedAt", "ValueType", "WebsiteTemplateId" },
                values: new object[,]
                {
                    { new Guid("f1000000-0000-4000-8000-000000000001"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Replaces the first hero slide's image. Recommended size ~1920x800px.", true, false, "heroImage", "Hero image", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Image", new Guid("e1000000-0000-4000-8000-000000000001") },
                    { new Guid("f1000000-0000-4000-8000-000000000002"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Replaces the first hero slide's heading text.", true, false, "heroHeadline", "Hero headline", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text", new Guid("e1000000-0000-4000-8000-000000000001") },
                    { new Guid("f1000000-0000-4000-8000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Recommended size ~1200x600px.", true, false, "promoBannerImage", "Promo banner image", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Image", new Guid("e1000000-0000-4000-8000-000000000001") },
                    { new Guid("f1000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, true, false, "promoBannerText", "Promo banner text", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text", new Guid("e1000000-0000-4000-8000-000000000001") },
                    { new Guid("f2000000-0000-4000-8000-000000000001"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Replaces the first hero slide's image. Recommended size ~1920x800px.", true, false, "heroImage", "Hero image", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Image", new Guid("e1000000-0000-4000-8000-000000000002") },
                    { new Guid("f2000000-0000-4000-8000-000000000002"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Replaces the first hero slide's heading text.", true, false, "heroHeadline", "Hero headline", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text", new Guid("e1000000-0000-4000-8000-000000000002") },
                    { new Guid("f2000000-0000-4000-8000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Recommended size ~1200x600px.", true, false, "promoBannerImage", "Promo banner image", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Image", new Guid("e1000000-0000-4000-8000-000000000002") },
                    { new Guid("f2000000-0000-4000-8000-000000000004"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, true, false, "promoBannerText", "Promo banner text", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Text", new Guid("e1000000-0000-4000-8000-000000000002") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_website_template_customizable_components_WebsiteTemplateId_K~",
                table: "website_template_customizable_components",
                columns: new[] { "WebsiteTemplateId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "website_template_customizable_components");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "BusinessHours",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "City",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "SocialLinks",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "State",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "WebsiteCustomizationValues",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "businesses");
        }
    }
}
