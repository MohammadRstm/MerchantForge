using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_acceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TermsVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrivacyPolicyVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcceptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_acceptances", x => x.Id);
                    table.CheckConstraint("CK_legal_acceptances_ExactlyOneOwner", "(`UserId` IS NULL) <> (`CustomerId` IS NULL)");
                    table.ForeignKey(
                        name: "FK_legal_acceptances_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_legal_acceptances_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_legal_acceptances_CustomerId",
                table: "legal_acceptances",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_legal_acceptances_UserId",
                table: "legal_acceptances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_acceptances");
        }
    }
}
