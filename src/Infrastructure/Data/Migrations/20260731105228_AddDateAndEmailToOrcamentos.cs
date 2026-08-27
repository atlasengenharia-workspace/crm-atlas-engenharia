using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDateAndEmailToOrcamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE orcamentos ADD COLUMN IF NOT EXISTS data date;
                ALTER TABLE orcamentos ADD COLUMN IF NOT EXISTS email text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "email",
                table: "orcamentos");

        }
    }
}
