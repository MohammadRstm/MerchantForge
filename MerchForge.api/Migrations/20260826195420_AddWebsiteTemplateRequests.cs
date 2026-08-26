using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteTemplateRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewWebsiteUrl",
                table: "website_templates",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "website_template_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequestedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WebsiteTemplateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomizationNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BuildStartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FinalWebsiteUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_template_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_website_template_requests_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_website_template_requests_website_templates_WebsiteTemplateId",
                        column: x => x.WebsiteTemplateId,
                        principalTable: "website_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000001"),
                column: "PreviewWebsiteUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000002"),
                column: "PreviewWebsiteUrl",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_website_template_requests_BusinessId",
                table: "website_template_requests",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_website_template_requests_Status",
                table: "website_template_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_website_template_requests_WebsiteTemplateId",
                table: "website_template_requests",
                column: "WebsiteTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "website_template_requests");

            migrationBuilder.DropColumn(
                name: "PreviewWebsiteUrl",
                table: "website_templates");
        }
    }
}
