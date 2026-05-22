using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MotorcycleContentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HighlightsJson",
                table: "motorcycles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSpecsJson",
                table: "motorcycles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighlightsJson",
                table: "motorcycles");

            migrationBuilder.DropColumn(
                name: "TechnicalSpecsJson",
                table: "motorcycles");
        }
    }
}
