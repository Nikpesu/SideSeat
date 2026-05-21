using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Registracija",
                table: "Vozila",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vozila",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Marka",
                table: "Vozila",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Boja",
                table: "Vozila",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PostanskiBroj",
                table: "Gradovi",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Gradovi",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Drzava",
                table: "Gradovi",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Gradovi ON;
                INSERT INTO Gradovi (Id, Drzava, Naziv, PostanskiBroj)
                SELECT v.Id, v.Drzava, v.Naziv, v.PostanskiBroj
                FROM (VALUES
                    (3, N'Hrvatska', N'Rijeka', N'51000'),
                    (4, N'Hrvatska', N'Osijek', N'31000'),
                    (5, N'Hrvatska', N'Zadar', N'23000'),
                    (6, N'Hrvatska', N'Pula', N'52100'),
                    (7, N'Hrvatska', N'Slavonski Brod', N'35000'),
                    (8, N'Hrvatska', N'Karlovac', N'47000'),
                    (9, N'Hrvatska', N'Varazdin', N'42000'),
                    (10, N'Hrvatska', N'Sibenik', N'22000'),
                    (11, N'Hrvatska', N'Dubrovnik', N'20000'),
                    (12, N'Hrvatska', N'Sisak', N'44000'),
                    (13, N'Hrvatska', N'Vukovar', N'32000'),
                    (14, N'Hrvatska', N'Bjelovar', N'43000'),
                    (15, N'Hrvatska', N'Koprivnica', N'48000'),
                    (16, N'Hrvatska', N'Cakovec', N'40000'),
                    (17, N'Hrvatska', N'Vinkovci', N'32100'),
                    (18, N'Hrvatska', N'Makarska', N'21300'),
                    (19, N'Hrvatska', N'Samobor', N'10430'),
                    (20, N'Hrvatska', N'Velika Gorica', N'10410'),
                    (21, N'Hrvatska', N'Kastela', N'21210'),
                    (22, N'Hrvatska', N'Pozega', N'34000')
                ) AS v(Id, Drzava, Naziv, PostanskiBroj)
                WHERE NOT EXISTS (SELECT 1 FROM Gradovi g WHERE g.Id = v.Id);
                SET IDENTITY_INSERT Gradovi OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Korisnici ON;
                INSERT INTO Korisnici (Id, Adresa, BrojMobitela, DatumRegistracije, Email, Ime, JeAktivan, KycBrojOsobne, KycBrojVozacke, KycDatumRodenja, KycOib, KycPodnesen, LozinkaHash, Prezime, Tip, VoziloId)
                SELECT v.Id, v.Adresa, v.BrojMobitela, v.DatumRegistracije, v.Email, v.Ime, v.JeAktivan, v.KycBrojOsobne, v.KycBrojVozacke, v.KycDatumRodenja, v.KycOib, v.KycPodnesen, v.LozinkaHash, v.Prezime, v.Tip, v.VoziloId
                FROM (VALUES
                    (4, N'Korzo 1, Rijeka', N'0941111111', CAST('2026-05-03T09:00:00' AS datetime2), N'petar@example.com', N'Petar', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Peric', 0, NULL),
                    (5, N'Tvrda 12, Osijek', N'0951111111', CAST('2026-05-03T09:30:00' AS datetime2), N'maja@example.com', N'Maja', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Matic', 1, NULL),
                    (6, N'Poluotok 3, Zadar', N'0961111111', CAST('2026-05-04T08:45:00' AS datetime2), N'luka@example.com', N'Luka', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Lukic', 3, NULL),
                    (7, N'Forum 5, Pula', N'0971111111', CAST('2026-05-04T10:00:00' AS datetime2), N'iva@example.com', N'Iva', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Ilic', 1, NULL),
                    (8, N'Centar 20, Karlovac', N'0981111111', CAST('2026-05-05T11:15:00' AS datetime2), N'dino@example.com', N'Dino', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Doric', 0, NULL),
                    (9, N'Centar 8, Sisak', N'0991111111', CAST('2026-05-06T12:30:00' AS datetime2), N'nikolina@example.com', N'Nikolina', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Ninic', 1, NULL),
                    (10, N'Riva 2, Sibenik', N'0912222222', CAST('2026-05-07T08:15:00' AS datetime2), N'filip@example.com', N'Filip', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Filic', 3, NULL),
                    (11, N'Stari grad 4, Dubrovnik', N'0923333333', CAST('2026-05-07T09:00:00' AS datetime2), N'sandra@example.com', N'Sandra', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Soric', 1, NULL),
                    (12, N'Centar 44, Varazdin', N'0934444444', CAST('2026-05-08T07:45:00' AS datetime2), N'tomislav@example.com', N'Tomislav', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Tomic', 0, NULL),
                    (13, N'Centar 5, Slavonski Brod', N'0945555555', CAST('2026-05-08T10:30:00' AS datetime2), N'marina@example.com', N'Marina', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Marinic', 1, NULL),
                    (14, N'Nova cesta 14, Samobor', N'0956666666', CAST('2026-05-09T08:00:00' AS datetime2), N'bruno@example.com', N'Bruno', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Brnic', 0, NULL),
                    (15, N'Luka 12, Makarska', N'0967777777', CAST('2026-05-09T11:00:00' AS datetime2), N'lea@example.com', N'Lea', CAST(1 AS bit), NULL, NULL, NULL, NULL, CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Lelic', 1, NULL)
                ) AS v(Id, Adresa, BrojMobitela, DatumRegistracije, Email, Ime, JeAktivan, KycBrojOsobne, KycBrojVozacke, KycDatumRodenja, KycOib, KycPodnesen, LozinkaHash, Prezime, Tip, VoziloId)
                WHERE NOT EXISTS (SELECT 1 FROM Korisnici k WHERE k.Id = v.Id);
                SET IDENTITY_INSERT Korisnici OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Voznje ON;
                INSERT INTO Voznje (Id, CijenaPoMjestu, OcekivaniDolazak, OdredisniGradId, Opis, Polazak, PolazniGradId, SlobodnaMjesta, Status, UkupnoMjesta, VozacId)
                SELECT v.Id, v.CijenaPoMjestu, v.OcekivaniDolazak, v.OdredisniGradId, v.Opis, v.Polazak, v.PolazniGradId, v.SlobodnaMjesta, v.Status, v.UkupnoMjesta, v.VozacId
                FROM (VALUES
                    (2, 12.00, CAST('2026-05-11T09:30:00' AS datetime2), 1, N'Rijeka - Zagreb', CAST('2026-05-11T07:00:00' AS datetime2), 3, 2, 0, 4, 4),
                    (3, 10.00, CAST('2026-05-11T18:00:00' AS datetime2), 2, N'Popodnevna voznja', CAST('2026-05-11T15:30:00' AS datetime2), 5, 3, 0, 4, 6),
                    (4, 8.00, CAST('2026-05-12T09:15:00' AS datetime2), 1, N'Karlovac -> Zagreb', CAST('2026-05-12T08:15:00' AS datetime2), 8, 1, 0, 3, 8),
                    (5, 9.50, CAST('2026-05-12T08:20:00' AS datetime2), 2, N'Jutarnji prijevoz', CAST('2026-05-12T06:50:00' AS datetime2), 10, 4, 0, 4, 10),
                    (6, 11.00, CAST('2026-05-12T15:20:00' AS datetime2), 1, N'Varazdin -> Zagreb', CAST('2026-05-12T14:00:00' AS datetime2), 9, 2, 0, 4, 12),
                    (7, 6.00, CAST('2026-05-13T08:50:00' AS datetime2), 1, N'Samobor shuttle', CAST('2026-05-13T08:00:00' AS datetime2), 19, 2, 0, 3, 14),
                    (8, 5.00, CAST('2026-05-13T17:55:00' AS datetime2), 20, N'ZG -> VG', CAST('2026-05-13T17:20:00' AS datetime2), 1, 3, 0, 4, 1),
                    (9, 10.00, CAST('2026-05-14T10:40:00' AS datetime2), 6, N'Rijeka -> Pula', CAST('2026-05-14T09:00:00' AS datetime2), 3, 4, 0, 4, 4),
                    (10, 4.50, CAST('2026-05-14T11:50:00' AS datetime2), 21, N'Split -> Kastela', CAST('2026-05-14T11:15:00' AS datetime2), 2, 2, 0, 4, 6),
                    (11, 9.00, CAST('2026-05-15T08:50:00' AS datetime2), 12, N'Karlovac -> Sisak', CAST('2026-05-15T07:40:00' AS datetime2), 8, 3, 0, 3, 8),
                    (12, 8.50, CAST('2026-05-15T14:55:00' AS datetime2), 5, N'Sibenik -> Zadar', CAST('2026-05-15T13:25:00' AS datetime2), 10, 1, 0, 4, 10),
                    (13, 6.00, CAST('2026-05-15T16:45:00' AS datetime2), 15, N'Varazdin -> Koprivnica', CAST('2026-05-15T16:00:00' AS datetime2), 9, 4, 0, 4, 12),
                    (14, 9.00, CAST('2026-05-16T09:40:00' AS datetime2), 8, N'Samobor -> Karlovac', CAST('2026-05-16T08:20:00' AS datetime2), 19, 2, 0, 3, 14),
                    (15, 16.00, CAST('2026-05-16T09:30:00' AS datetime2), 4, N'Zagreb -> Osijek', CAST('2026-05-16T06:30:00' AS datetime2), 1, 3, 0, 4, 1),
                    (16, 18.00, CAST('2026-05-16T21:00:00' AS datetime2), 10, N'Rijeka -> Sibenik', CAST('2026-05-16T17:10:00' AS datetime2), 3, 4, 0, 4, 4),
                    (17, 7.00, CAST('2026-05-17T10:10:00' AS datetime2), 18, N'Split -> Makarska', CAST('2026-05-17T09:00:00' AS datetime2), 2, 2, 0, 4, 6),
                    (18, 14.00, CAST('2026-05-17T15:05:00' AS datetime2), 22, N'Karlovac -> Pozega', CAST('2026-05-17T12:20:00' AS datetime2), 8, 2, 0, 3, 8),
                    (19, 20.00, CAST('2026-05-18T11:30:00' AS datetime2), 11, N'Sibenik -> Dubrovnik', CAST('2026-05-18T07:00:00' AS datetime2), 10, 1, 0, 4, 10),
                    (20, 5.00, CAST('2026-05-18T15:35:00' AS datetime2), 16, N'Varazdin -> Cakovec', CAST('2026-05-18T15:00:00' AS datetime2), 9, 3, 0, 4, 12),
                    (21, 6.00, CAST('2026-05-19T07:35:00' AS datetime2), 1, N'Jutarnji commute', CAST('2026-05-19T06:50:00' AS datetime2), 19, 3, 0, 3, 14),
                    (22, 15.00, CAST('2026-05-19T17:00:00' AS datetime2), 7, N'Zagreb -> Slavonski Brod', CAST('2026-05-19T14:30:00' AS datetime2), 1, 2, 0, 4, 1),
                    (23, 23.00, CAST('2026-05-20T11:45:00' AS datetime2), 13, N'Rijeka -> Vukovar', CAST('2026-05-20T05:45:00' AS datetime2), 3, 4, 0, 4, 4),
                    (24, 9.00, CAST('2026-05-20T20:00:00' AS datetime2), 5, N'Split -> Zadar vecernja', CAST('2026-05-20T18:15:00' AS datetime2), 2, 3, 0, 4, 6),
                    (25, 10.00, CAST('2026-05-21T11:00:00' AS datetime2), 14, N'Karlovac -> Bjelovar', CAST('2026-05-21T09:10:00' AS datetime2), 8, 3, 0, 3, 8)
                ) AS v(Id, CijenaPoMjestu, OcekivaniDolazak, OdredisniGradId, Opis, Polazak, PolazniGradId, SlobodnaMjesta, Status, UkupnoMjesta, VozacId)
                WHERE NOT EXISTS (SELECT 1 FROM Voznje vo WHERE vo.Id = v.Id);
                SET IDENTITY_INSERT Voznje OFF;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Voznje",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Gradovi",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Korisnici",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.AlterColumn<string>(
                name: "Registracija",
                table: "Vozila",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(24)",
                oldMaxLength: 24);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vozila",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Marka",
                table: "Vozila",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Boja",
                table: "Vozila",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "PostanskiBroj",
                table: "Gradovi",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Gradovi",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Drzava",
                table: "Gradovi",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
