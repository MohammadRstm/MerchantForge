using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class ChangedSystemRoleToSysteRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemRole",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "SystemRoleId",
                table: "users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "SystemRoles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemRoleId",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "SystemRole",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "SystemRoles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
