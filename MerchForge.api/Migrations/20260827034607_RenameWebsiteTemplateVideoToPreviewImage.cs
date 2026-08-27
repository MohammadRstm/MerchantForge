using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerchForge.api.Migrations
{
    /// <inheritdoc />
    public partial class RenameWebsiteTemplateVideoToPreviewImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoPreviewUrl",
                table: "website_templates",
                newName: "PreviewImageUrl");

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000001"),
                column: "PreviewImageUrl",
                value: "/images/templates/coming-soon.jpg");

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000002"),
                column: "PreviewImageUrl",
                value: "/images/templates/coming-soon.jpg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreviewImageUrl",
                table: "website_templates",
                newName: "VideoPreviewUrl");

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000001"),
                column: "VideoPreviewUrl",
                value: "/videos/templates/coming-soon.mp4");

            migrationBuilder.UpdateData(
                table: "website_templates",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-4000-8000-000000000002"),
                column: "VideoPreviewUrl",
                value: "/videos/templates/coming-soon.mp4");
        }
    }
}
