using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MotorcycleDetailContentAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap legacy MotorcycleCategory ints → new enum values (idempotent CASE).
            // Old: 0 Scooter, 1 Sport, 2 Naked, 3 Adventure, 4 Cub, 5 Electric, 99 Other
            // New: 0 Scooter, 1 XeSo, 2 ConTay, 3 PhanKhoiLon, 4 Electric
            migrationBuilder.Sql("""
                UPDATE motorcycles SET "Category" = CASE "Category"
                    WHEN 0 THEN 0
                    WHEN 1 THEN 2
                    WHEN 2 THEN 2
                    WHEN 3 THEN 3
                    WHEN 4 THEN 1
                    WHEN 5 THEN 4
                    WHEN 99 THEN 0
                    ELSE 0
                END;
                """);

            migrationBuilder.CreateTable(
                name: "motorcycle_features",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorcycle_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_motorcycle_features_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motorcycle_spin_frames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FrameIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorcycle_spin_frames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_motorcycle_spin_frames_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motorcycle_technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorcycle_technologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_motorcycle_technologies_motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_motorcycle_features_MotorcycleId",
                table: "motorcycle_features",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_motorcycle_spin_frames_MotorcycleId_FrameIndex",
                table: "motorcycle_spin_frames",
                columns: new[] { "MotorcycleId", "FrameIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_motorcycle_technologies_MotorcycleId",
                table: "motorcycle_technologies",
                column: "MotorcycleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "motorcycle_features");

            migrationBuilder.DropTable(
                name: "motorcycle_spin_frames");

            migrationBuilder.DropTable(
                name: "motorcycle_technologies");
        }
    }
}
