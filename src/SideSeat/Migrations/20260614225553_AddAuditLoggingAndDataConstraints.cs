using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLoggingAndDataConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici");

            migrationBuilder.DropForeignKey(
                name: "FK_Vozila_Korisnici_VlasnikId",
                table: "Vozila");

            migrationBuilder.AlterColumn<decimal>(
                name: "ProsjecnaPotrosnja",
                table: "Vozila",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KorisnikId = table.Column<int>(type: "int", nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vozila_Registracija",
                table: "Vozila",
                column: "Registracija",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradovi_Naziv_Drzava",
                table: "Gradovi",
                columns: new[] { "Naziv", "Drzava" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici",
                column: "VoziloId",
                principalTable: "Vozila",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vozila_Korisnici_VlasnikId",
                table: "Vozila",
                column: "VlasnikId",
                principalTable: "Korisnici",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici");

            migrationBuilder.DropForeignKey(
                name: "FK_Vozila_Korisnici_VlasnikId",
                table: "Vozila");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Vozila_Registracija",
                table: "Vozila");

            migrationBuilder.DropIndex(
                name: "IX_Gradovi_Naziv_Drzava",
                table: "Gradovi");

            migrationBuilder.AlterColumn<decimal>(
                name: "ProsjecnaPotrosnja",
                table: "Vozila",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,2)",
                oldPrecision: 8,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici",
                column: "VoziloId",
                principalTable: "Vozila",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vozila_Korisnici_VlasnikId",
                table: "Vozila",
                column: "VlasnikId",
                principalTable: "Korisnici",
                principalColumn: "Id");
        }
    }
}
