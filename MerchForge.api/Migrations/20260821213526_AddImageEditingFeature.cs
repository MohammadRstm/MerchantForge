using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddImageEditingFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "image_edit_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Prompt = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InputImageUrls = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutputImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_edit_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_image_edit_jobs_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "features",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Key", "Name", "SupportsCreditPurchase", "UpdatedAt" },
                values: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit a product photo by describing the change you want instead of using an image editor.", true, "ai.image_editing", "AI Image Editing", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "feature_credit_packages",
                columns: new[] { "Id", "CreatedAt", "Credits", "Currency", "FeatureId", "IsActive", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-4000-8000-000000000201"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, "USD", new Guid("f0000000-0000-4000-8000-000000000002"), true, "Starter", 5m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-4000-8000-000000000202"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100, "USD", new Guid("f0000000-0000-4000-8000-000000000002"), true, "Pro", 20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_image_edit_jobs_BusinessId",
                table: "image_edit_jobs",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "image_edit_jobs");

            migrationBuilder.DeleteData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000201"));

            migrationBuilder.DeleteData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000202"));

            migrationBuilder.DeleteData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000002"));
        }
    }
}
