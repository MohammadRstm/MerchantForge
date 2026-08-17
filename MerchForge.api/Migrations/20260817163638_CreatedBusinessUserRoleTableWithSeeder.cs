using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class CreatedBusinessUserRoleTableWithSeeder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "business_users");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessUserRoleId",
                table: "business_users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "business_users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "BusinessUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUserRoles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "BusinessUserRoles",
                columns: new[] { "Id", "Role" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Owner" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Admin" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Member" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_users_BusinessUserRoleId",
                table: "business_users",
                column: "BusinessUserRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_business_users_BusinessUserRoles_BusinessUserRoleId",
                table: "business_users",
                column: "BusinessUserRoleId",
                principalTable: "BusinessUserRoles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_business_users_BusinessUserRoles_BusinessUserRoleId",
                table: "business_users");

            migrationBuilder.DropTable(
                name: "BusinessUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_business_users_BusinessUserRoleId",
                table: "business_users");

            migrationBuilder.DropColumn(
                name: "BusinessUserRoleId",
                table: "business_users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "business_users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "business_users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
