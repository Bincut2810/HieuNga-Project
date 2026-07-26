using System;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HieuNga.Infrastructure.Persistence.Migrations;

[DbContext(typeof(HieuNgaDbContext))]
[Migration("20260726160000_MotorcycleHeroImageUrl")]
partial class MotorcycleHeroImageUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "HeroImageUrl",
            table: "motorcycles",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HeroImageUrl",
            table: "motorcycles");
    }
}
