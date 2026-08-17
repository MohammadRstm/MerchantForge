using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemRoles",
                columns: new[] { "Id", "Role" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "SuperAdmin" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Admin" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "User" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "SystemRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "SystemRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
