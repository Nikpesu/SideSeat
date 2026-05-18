using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class UserAuthAndKycFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresa",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KycBrojOsobne",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KycBrojVozacke",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KycDatumRodenja",
                table: "Korisnici",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KycOib",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KycPodnesen",
                table: "Korisnici",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LozinkaHash",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Adresa", "KycBrojOsobne", "KycBrojVozacke", "KycDatumRodenja", "KycOib", "KycPodnesen", "LozinkaHash" },
                values: new object[] { "Ilica 10, Zagreb", "12345678", "HR-VOZ-001", new DateTime(1990, 4, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "12345678901", true, "WjgcARlXlANvPrwzymGupw==.mAJGLSfwA1qBVtNB5RA7flpqKTF6m4GaYykrX7DvfRM=" });

            migrationBuilder.UpdateData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Adresa", "KycBrojOsobne", "KycBrojVozacke", "KycDatumRodenja", "KycOib", "KycPodnesen", "LozinkaHash" },
                values: new object[] { "Marmontova 21, Split", null, null, null, null, false, "d+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=" });

            migrationBuilder.UpdateData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Adresa", "KycBrojOsobne", "KycBrojVozacke", "KycDatumRodenja", "KycOib", "KycPodnesen", "LozinkaHash" },
                values: new object[] { "Savska 100, Zagreb", null, null, null, null, true, "UJon3KrOrz+TJuEAH49PBA==.mtr30yYsJ5J4MS/edvNKLbZ/aOhiZ+W0gPp24rGz0JY=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adresa",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "KycBrojOsobne",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "KycBrojVozacke",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "KycDatumRodenja",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "KycOib",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "KycPodnesen",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "LozinkaHash",
                table: "Korisnici");
        }
    }
}
