using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropMotorcycleGalleryAndHeroV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropColumn(
                name: "HeroImageUrl",
                table: "motorcycles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeroImageUrl",
                table: "motorcycles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AltText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_assets_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "motorcycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_MotorcycleId",
                table: "media_assets",
                column: "MotorcycleId");
        }
    }
}
