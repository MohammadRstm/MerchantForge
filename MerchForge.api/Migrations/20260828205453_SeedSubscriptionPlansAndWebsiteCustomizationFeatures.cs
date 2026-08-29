using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class SeedSubscriptionPlansAndWebsiteCustomizationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000101"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000102"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000001"),
                column: "SupportsCreditPurchase",
                value: false);

            migrationBuilder.InsertData(
                table: "features",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Key", "Name", "SupportsCreditPurchase", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Logo, favicon, brand color, tagline, description, and contact/address details on your storefront.", true, "website_customization.basic", "Basic Website Customization", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Social links, business hours, and per-template storefront fields (hero image, promo banner, etc.).", true, "website_customization.advanced", "Advanced Website Customization", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingInterval", "CreatedAt", "Currency", "Description", "IsActive", "IsCustom", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("d1000000-0000-4000-8000-000000000001"), "Monthly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Unlimited AI product creation, 40 image-edit credits/mo, and basic website branding.", true, false, "Starter", 19m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1000000-0000-4000-8000-000000000002"), "Yearly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Unlimited AI product creation, 40 image-edit credits/mo, and basic website branding.", true, false, "Starter", 180m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1000000-0000-4000-8000-000000000003"), "Monthly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Everything in Starter, plus 150 image-edit credits/mo and advanced website customization.", true, false, "Growth", 49m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1000000-0000-4000-8000-000000000004"), "Yearly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Everything in Starter, plus 150 image-edit credits/mo and advanced website customization.", true, false, "Growth", 468m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1000000-0000-4000-8000-000000000005"), "Monthly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Everything in Growth, plus 400 image-edit credits/mo.", true, false, "Pro", 99m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1000000-0000-4000-8000-000000000006"), "Yearly", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Everything in Growth, plus 400 image-edit credits/mo.", true, false, "Pro", 948m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "plan_features",
                columns: new[] { "FeatureId", "SubscriptionPlanId", "Limit" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000001"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000001"), 40 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000001"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000002"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000002"), 40 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000002"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000003"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000003"), 150 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000003"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000003"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000004"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000004"), 150 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000004"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000004"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000005"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000005"), 400 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000005"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000005"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000006"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000006"), 400 },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000006"), null },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000006"), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000001") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000001") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000001") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000003") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000003") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000003") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000003") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000004") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000004") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000004") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000004") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000005") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000005") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000005") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000005") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000001"), new Guid("d1000000-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000002"), new Guid("d1000000-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000003"), new Guid("d1000000-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                table: "plan_features",
                keyColumns: new[] { "FeatureId", "SubscriptionPlanId" },
                keyValues: new object[] { new Guid("f0000000-0000-4000-8000-000000000004"), new Guid("d1000000-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-4000-8000-000000000006"));

            migrationBuilder.UpdateData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000101"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "feature_credit_packages",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000102"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "features",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-4000-8000-000000000001"),
                column: "SupportsCreditPurchase",
                value: true);
        }
    }
}
