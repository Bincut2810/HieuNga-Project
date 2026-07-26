using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HieuNga.Infrastructure.Persistence;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations;

[DbContext(typeof(HieuNgaDbContext))]
[Migration("20260726170000_SpinFramesToSixAngles")]
public partial class SpinFramesToSixAngles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Soft-delete legacy 36-frame rows — dealerships re-upload the six angles.
        migrationBuilder.Sql("""
            UPDATE motorcycle_spin_frames
            SET "IsDeleted" = TRUE, "UpdatedAt" = NOW()
            WHERE "IsDeleted" = FALSE;
            """);

        migrationBuilder.DropIndex(
            name: "IX_motorcycle_spin_frames_MotorcycleId_FrameIndex",
            table: "motorcycle_spin_frames");

        migrationBuilder.RenameColumn(
            name: "FrameIndex",
            table: "motorcycle_spin_frames",
            newName: "Angle");

        migrationBuilder.CreateIndex(
            name: "IX_motorcycle_spin_frames_MotorcycleId_Angle",
            table: "motorcycle_spin_frames",
            columns: new[] { "MotorcycleId", "Angle" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_motorcycle_spin_frames_MotorcycleId_Angle",
            table: "motorcycle_spin_frames");

        migrationBuilder.RenameColumn(
            name: "Angle",
            table: "motorcycle_spin_frames",
            newName: "FrameIndex");

        migrationBuilder.CreateIndex(
            name: "IX_motorcycle_spin_frames_MotorcycleId_FrameIndex",
            table: "motorcycle_spin_frames",
            columns: new[] { "MotorcycleId", "FrameIndex" });
    }
}
