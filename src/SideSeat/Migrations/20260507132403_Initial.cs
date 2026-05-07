using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gradovi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Drzava = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostanskiBroj = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gradovi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrojMobitela = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatumRegistracije = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    JeAktivan = table.Column<bool>(type: "bit", nullable: false),
                    VoziloId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vozila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Registracija = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GodinaProizvodnje = table.Column<int>(type: "int", nullable: false),
                    BrojSjedala = table.Column<int>(type: "int", nullable: false),
                    Boja = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProsjecnaPotrosnja = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VlasnikId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vozila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vozila_Korisnici_VlasnikId",
                        column: x => x.VlasnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Voznje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VozacId = table.Column<int>(type: "int", nullable: false),
                    PolazniGradId = table.Column<int>(type: "int", nullable: false),
                    OdredisniGradId = table.Column<int>(type: "int", nullable: false),
                    Polazak = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OcekivaniDolazak = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CijenaPoMjestu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UkupnoMjesta = table.Column<int>(type: "int", nullable: false),
                    SlobodnaMjesta = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voznje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voznje_Gradovi_OdredisniGradId",
                        column: x => x.OdredisniGradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voznje_Gradovi_PolazniGradId",
                        column: x => x.PolazniGradId,
                        principalTable: "Gradovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voznje_Korisnici_VozacId",
                        column: x => x.VozacId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rezervacije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoznjaId = table.Column<int>(type: "int", nullable: false),
                    PutnikId = table.Column<int>(type: "int", nullable: false),
                    BrojMjesta = table.Column<int>(type: "int", nullable: false),
                    CijenaUkupno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VrijemeRezervacije = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Napomena = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervacije", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rezervacije_Korisnici_PutnikId",
                        column: x => x.PutnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rezervacije_Voznje_VoznjaId",
                        column: x => x.VoznjaId,
                        principalTable: "Voznje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ocjene",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    BrojZvjezdica = table.Column<int>(type: "int", nullable: false),
                    Komentar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kreirano = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocjene", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocjene_Korisnici_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ocjene_Rezervacije_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Placanja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VrijemePlacanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NacinPlacanja = table.Column<int>(type: "int", nullable: false),
                    Uspjesno = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placanja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Placanja_Rezervacije_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Korisnici_VoziloId",
                table: "Korisnici",
                column: "VoziloId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocjene_AutorId",
                table: "Ocjene",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocjene_RezervacijaId",
                table: "Ocjene",
                column: "RezervacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Placanja_RezervacijaId",
                table: "Placanja",
                column: "RezervacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacije_PutnikId",
                table: "Rezervacije",
                column: "PutnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacije_VoznjaId",
                table: "Rezervacije",
                column: "VoznjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vozila_VlasnikId",
                table: "Vozila",
                column: "VlasnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Voznje_OdredisniGradId",
                table: "Voznje",
                column: "OdredisniGradId");

            migrationBuilder.CreateIndex(
                name: "IX_Voznje_PolazniGradId",
                table: "Voznje",
                column: "PolazniGradId");

            migrationBuilder.CreateIndex(
                name: "IX_Voznje_VozacId",
                table: "Voznje",
                column: "VozacId");

            migrationBuilder.AddForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici",
                column: "VoziloId",
                principalTable: "Vozila",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Korisnici_Vozila_VoziloId",
                table: "Korisnici");

            migrationBuilder.DropTable(
                name: "Ocjene");

            migrationBuilder.DropTable(
                name: "Placanja");

            migrationBuilder.DropTable(
                name: "Rezervacije");

            migrationBuilder.DropTable(
                name: "Voznje");

            migrationBuilder.DropTable(
                name: "Gradovi");

            migrationBuilder.DropTable(
                name: "Vozila");

            migrationBuilder.DropTable(
                name: "Korisnici");
        }
    }
}
