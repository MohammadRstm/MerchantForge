using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureCreditPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SupportsCreditPurchase",
                table: "features",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "business_feature_credits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FeatureId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreditsRemaining = table.Column<int>(type: "int", nullable: false),
                    CreditsGrantedTotal = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_feature_credits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_business_feature_credits_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_business_feature_credits_features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feature_credit_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FeatureId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_credit_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feature_credit_packages_features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feature_credit_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BusinessFeatureCreditId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    FeatureCreditPackageId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Reference = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_credit_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feature_credit_transactions_business_feature_credits_Busines~",
                        column: x => x.BusinessFeatureCreditId,
                        principalTable: "business_feature_credits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_feature_credit_transactions_feature_credit_packages_FeatureC~",
                        column: x => x.FeatureCreditPackageId,
                        principalTable: "feature_credit_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "features",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Key", "Name", "SupportsCreditPurchase", "UpdatedAt" },
                values: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Draft a product through conversation instead of filling in the form by hand.", true, "ai.product_generation", "AI Product Creation", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "feature_credit_packages",
                columns: new[] { "Id", "CreatedAt", "Credits", "Currency", "FeatureId", "IsActive", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-4000-8000-000000000101"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 50, "USD", new Guid("f0000000-0000-4000-8000-000000000001"), true, "Starter", 5m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-4000-8000-000000000102"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200, "USD", new Guid("f0000000-0000-4000-8000-000000000001"), true, "Pro", 15m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_feature_credits_BusinessId_FeatureId",
                table: "business_feature_credits",
                columns: new[] { "BusinessId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_feature_credits_FeatureId",
                table: "business_feature_credits",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_feature_credit_packages_FeatureId",
                table: "feature_credit_packages",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_feature_credit_transactions_BusinessFeatureCreditId",
                table: "feature_credit_transactions",
                column: "BusinessFeatureCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_feature_credit_transactions_FeatureCreditPackageId",
                table: "feature_credit_transactions",
                column: "FeatureCreditPackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_credit_transactions");

            migrationBuilder.DropTable(
                name: "business_feature_credits");

            migrationBuilder.DropTable(
                name: "feature_credit_packages");

            migrationBuilder.DeleteData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000001"));

            migrationBuilder.DropColumn(
                name: "SupportsCreditPurchase",
                table: "features");
        }
    }
}
