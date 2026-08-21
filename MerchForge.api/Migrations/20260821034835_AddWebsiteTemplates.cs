using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WebsiteTemplateChosenAt",
                table: "businesses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WebsiteTemplateId",
                table: "businesses",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "website_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessDomainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VideoPreviewUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_website_templates_business_domains_BusinessDomainId",
                        column: x => x.BusinessDomainId,
                        principalTable: "business_domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "website_templates",
                columns: new[] { "Id", "BusinessDomainId", "CreatedAt", "DisplayOrder", "IsActive", "Label", "Name", "UpdatedAt", "VideoPreviewUrl" },
                values: new object[,]
                {
                    { new Guid("e1000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Vineta Fashion", "fashion-template-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/videos/templates/coming-soon.mp4" },
                    { new Guid("e1000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Vineta Electronics", "electronic-template-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/videos/templates/coming-soon.mp4" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_businesses_WebsiteTemplateId",
                table: "businesses",
                column: "WebsiteTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_website_templates_BusinessDomainId",
                table: "website_templates",
                column: "BusinessDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_website_templates_Name",
                table: "website_templates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_businesses_website_templates_WebsiteTemplateId",
                table: "businesses",
                column: "WebsiteTemplateId",
                principalTable: "website_templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_businesses_website_templates_WebsiteTemplateId",
                table: "businesses");

            migrationBuilder.DropTable(
                name: "website_templates");

            migrationBuilder.DropIndex(
                name: "IX_businesses_WebsiteTemplateId",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "WebsiteTemplateChosenAt",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "WebsiteTemplateId",
                table: "businesses");
        }
    }
}
