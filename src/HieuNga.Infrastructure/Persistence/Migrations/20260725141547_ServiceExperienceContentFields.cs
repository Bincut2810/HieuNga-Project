using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ServiceExperienceContentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaqJson",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GalleryJson",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroImageUrl",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessJson",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhenToUseJson",
                table: "service_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaqJson",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "GalleryJson",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "HeroImageUrl",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "ProcessJson",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "WhenToUseJson",
                table: "service_items");
        }
    }
}
