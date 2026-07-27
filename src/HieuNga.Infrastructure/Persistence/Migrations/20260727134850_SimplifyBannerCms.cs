using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyBannerCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badge",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "CtaText",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "CtaUrl",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "MobileImageUrl",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "OverlayStrength",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "SecondaryCtaText",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "SecondaryCtaUrl",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "banners");

            migrationBuilder.DropColumn(
                name: "TextAlignment",
                table: "banners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Badge",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaText",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaUrl",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "banners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileImageUrl",
                table: "banners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverlayStrength",
                table: "banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "banners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TextAlignment",
                table: "banners",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
