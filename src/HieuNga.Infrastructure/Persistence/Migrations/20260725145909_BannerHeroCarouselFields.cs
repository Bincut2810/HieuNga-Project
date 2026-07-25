using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BannerHeroCarouselFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Badge",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverlayStrength",
                table: "banners",
                type: "integer",
                nullable: false,
                defaultValue: 65);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryCtaText",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryCtaUrl",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TextAlignment",
                table: "banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badge",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "OverlayStrength",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "SecondaryCtaText",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "SecondaryCtaUrl",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "TextAlignment",
                table: "banners");
        }
    }
}
