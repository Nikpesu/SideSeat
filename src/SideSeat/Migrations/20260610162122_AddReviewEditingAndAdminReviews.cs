using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewEditingAndAdminReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Administratorska",
                table: "Ocjene",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Uredeno",
                table: "Ocjene",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Administratorska",
                table: "Ocjene");

            migrationBuilder.DropColumn(
                name: "Uredeno",
                table: "Ocjene");
        }
    }
}
