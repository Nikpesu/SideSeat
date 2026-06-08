using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obavijesti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Naslov = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Poruka = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Kreirano = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Procitano = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obavijesti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Obavijesti_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Obavijesti_KorisnikId",
                table: "Obavijesti",
                column: "KorisnikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Obavijesti");
        }
    }
}
