using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMaintenanceBookingV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_bookings_branches_BranchId",
                table: "maintenance_bookings");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_bookings_BranchId",
                table: "maintenance_bookings");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "maintenance_bookings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "maintenance_bookings");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "maintenance_bookings");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "maintenance_bookings");

            migrationBuilder.AlterColumn<string>(
                name: "PreferredTime",
                table: "maintenance_bookings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MotorcycleModel",
                table: "maintenance_bookings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredTime",
                table: "maintenance_bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MotorcycleModel",
                table: "maintenance_bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "maintenance_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "maintenance_bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "maintenance_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "maintenance_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_bookings_BranchId",
                table: "maintenance_bookings",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_bookings_branches_BranchId",
                table: "maintenance_bookings",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");
        }
    }
}
