using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Gradovi",
                columns: new[] { "Id", "Drzava", "Naziv", "PostanskiBroj" },
                values: new object[,]
                {
                    { 1, "Hrvatska", "Zagreb", "10000" },
                    { 2, "Hrvatska", "Split", "21000" }
                });

            migrationBuilder.InsertData(
                table: "Korisnici",
                columns: new[] { "Id", "BrojMobitela", "DatumRegistracije", "Email", "Ime", "JeAktivan", "Prezime", "Tip", "VoziloId" },
                values: new object[,]
                {
                    { 1, "0911111111", new DateTime(2026, 5, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), "marko@example.com", "Marko", true, "Maric", 0, null },
                    { 2, "0922222222", new DateTime(2026, 5, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "ivana@example.com", "Ivana", true, "Ivic", 1, null },
                    { 3, "0933333333", new DateTime(2026, 5, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), "admin@example.com", "Ana", true, "Admin", 2, null }
                });

            migrationBuilder.InsertData(
                table: "Vozila",
                columns: new[] { "Id", "Boja", "BrojSjedala", "GodinaProizvodnje", "Marka", "Model", "ProsjecnaPotrosnja", "Registracija", "VlasnikId" },
                values: new object[] { 1, "Siva", 5, 2021, "Skoda", "Octavia", 6.5m, "ZG-1234-AA", 1 });

            migrationBuilder.InsertData(
                table: "Voznje",
                columns: new[] { "Id", "CijenaPoMjestu", "OcekivaniDolazak", "OdredisniGradId", "Opis", "Polazak", "PolazniGradId", "SlobodnaMjesta", "Status", "UkupnoMjesta", "VozacId" },
                values: new object[] { 1, 15.00m, new DateTime(2026, 5, 10, 12, 0, 0, 0, DateTimeKind.Unspecified), 2, "Jutarnja voznja", new DateTime(2026, 5, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), 1, 3, 0, 4, 1 });

            migrationBuilder.InsertData(
                table: "Rezervacije",
                columns: new[] { "Id", "BrojMjesta", "CijenaUkupno", "Napomena", "PutnikId", "Status", "VoznjaId", "VrijemeRezervacije" },
                values: new object[] { 1, 1, 15.00m, "Bez prtljage", 2, 0, 1, new DateTime(2026, 5, 2, 9, 30, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Ocjene",
                columns: new[] { "Id", "AutorId", "BrojZvjezdica", "Komentar", "Kreirano", "RezervacijaId" },
                values: new object[] { 1, 2, 5, "Odlicno", new DateTime(2026, 5, 12, 14, 0, 0, 0, DateTimeKind.Unspecified), 1 });

            migrationBuilder.InsertData(
                table: "Placanja",
                columns: new[] { "Id", "Iznos", "NacinPlacanja", "RezervacijaId", "Uspjesno", "VrijemePlacanja" },
                values: new object[] { 1, 15.00m, 1, 1, true, new DateTime(2026, 5, 2, 10, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Ocjene",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Placanja",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Vozila",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rezervacije",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
