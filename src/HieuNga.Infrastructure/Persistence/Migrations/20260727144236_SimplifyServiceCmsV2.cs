using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyServiceCmsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "DetailDescription",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationText",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "EstimatedPriceText",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "FaqJson",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "HeroImageUrl",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "IncludesJson",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "OgImageUrl",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "PriceNote",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailDescription",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedDurationText",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedPriceText",
                table: "service_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FaqJson",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroImageUrl",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncludesJson",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "service_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgImageUrl",
                table: "service_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceNote",
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
    }
}
