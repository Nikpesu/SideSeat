using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class PersistSeedDataInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Korisnici ON;
                INSERT INTO Korisnici (Id, Adresa, BrojMobitela, DatumRegistracije, Email, Ime, JeAktivan, KycPodnesen, LozinkaHash, Prezime, Tip, VoziloId)
                SELECT v.Id, v.Adresa, v.BrojMobitela, v.DatumRegistracije, v.Email, v.Ime, v.JeAktivan, v.KycPodnesen, v.LozinkaHash, v.Prezime, v.Tip, NULL
                FROM (VALUES
                    (16, N'Centar 1, Vinkovci', N'0977770001', CAST('2026-05-10T09:00:00' AS datetime2), N'korisnik16@example.com', N'Karlo', CAST(1 AS bit), CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Kovacic', 0),
                    (17, N'Centar 2, Pozega', N'0977770002', CAST('2026-05-10T09:10:00' AS datetime2), N'korisnik17@example.com', N'Nika', CAST(1 AS bit), CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Novak', 1),
                    (18, N'Centar 3, Cakovec', N'0977770003', CAST('2026-05-10T09:20:00' AS datetime2), N'korisnik18@example.com', N'Matej', CAST(1 AS bit), CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Matic', 0),
                    (19, N'Centar 4, Sisak', N'0977770004', CAST('2026-05-10T09:30:00' AS datetime2), N'korisnik19@example.com', N'Sara', CAST(1 AS bit), CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Sekulic', 1),
                    (20, N'Centar 5, Bjelovar', N'0977770005', CAST('2026-05-10T09:40:00' AS datetime2), N'korisnik20@example.com', N'Petra', CAST(1 AS bit), CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Peric', 0),
                    (21, N'Centar 6, Karlovac', N'0977770006', CAST('2026-05-10T09:50:00' AS datetime2), N'korisnik21@example.com', N'Ante', CAST(1 AS bit), CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Anic', 1),
                    (22, N'Centar 7, Osijek', N'0977770007', CAST('2026-05-10T10:00:00' AS datetime2), N'korisnik22@example.com', N'Bruna', CAST(1 AS bit), CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Boric', 0),
                    (23, N'Centar 8, Split', N'0977770008', CAST('2026-05-10T10:10:00' AS datetime2), N'korisnik23@example.com', N'Leo', CAST(1 AS bit), CAST(0 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Lukic', 1),
                    (24, N'Centar 9, Zagreb', N'0977770009', CAST('2026-05-10T10:20:00' AS datetime2), N'korisnik24@example.com', N'Ivona', CAST(1 AS bit), CAST(1 AS bit), N'd+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=', N'Ivanic', 0)
                ) AS v(Id, Adresa, BrojMobitela, DatumRegistracije, Email, Ime, JeAktivan, KycPodnesen, LozinkaHash, Prezime, Tip)
                WHERE NOT EXISTS (SELECT 1 FROM Korisnici k WHERE k.Id = v.Id);
                SET IDENTITY_INSERT Korisnici OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Vozila ON;
                INSERT INTO Vozila (Id, Marka, Model, Registracija, GodinaProizvodnje, BrojSjedala, Boja, ProsjecnaPotrosnja, VlasnikId)
                SELECT v.Id, v.Marka, v.Model, v.Registracija, v.GodinaProizvodnje, v.BrojSjedala, v.Boja, v.ProsjecnaPotrosnja, v.VlasnikId
                FROM (VALUES
                    (2, N'VW', N'Golf', N'ST-2002-AA', 2020, 5, N'Crna', CAST(5.8 AS decimal(18,2)), 4),
                    (3, N'Renault', N'Clio', N'ZG-3003-BB', 2019, 5, N'Bijela', CAST(5.1 AS decimal(18,2)), 6),
                    (4, N'Opel', N'Astra', N'RI-4004-CC', 2018, 5, N'Siva', CAST(5.9 AS decimal(18,2)), 8),
                    (5, N'Peugeot', N'308', N'OS-5005-DD', 2021, 5, N'Plava', CAST(5.6 AS decimal(18,2)), 10),
                    (6, N'Toyota', N'Corolla', N'KA-6006-EE', 2022, 5, N'Srebrna', CAST(5.2 AS decimal(18,2)), 12),
                    (7, N'Seat', N'Leon', N'ZG-7007-FF', 2020, 5, N'Crvena', CAST(5.7 AS decimal(18,2)), 14),
                    (8, N'Hyundai', N'i30', N'VK-8008-GG', 2021, 5, N'Plava', CAST(5.4 AS decimal(18,2)), 16),
                    (9, N'Kia', N'Ceed', N'PO-9009-HH', 2020, 5, N'Crna', CAST(5.5 AS decimal(18,2)), 18),
                    (10, N'Mazda', N'3', N'CK-1010-II', 2019, 5, N'Bijela', CAST(6.0 AS decimal(18,2)), 20),
                    (11, N'Ford', N'Focus', N'ZG-1111-JJ', 2021, 5, N'Siva', CAST(5.9 AS decimal(18,2)), 22),
                    (12, N'Skoda', N'Fabia', N'OS-1212-KK', 2018, 5, N'Zelena', CAST(5.0 AS decimal(18,2)), 24),
                    (13, N'Dacia', N'Sandero', N'ST-1313-LL', 2017, 5, N'Bijela', CAST(5.3 AS decimal(18,2)), 1),
                    (14, N'Citroen', N'C4', N'RI-1414-MM', 2020, 5, N'Plava', CAST(5.8 AS decimal(18,2)), 4),
                    (15, N'Nissan', N'Qashqai', N'ZG-1515-NN', 2022, 5, N'Crna', CAST(6.4 AS decimal(18,2)), 6),
                    (16, N'Honda', N'Civic', N'KA-1616-OO', 2021, 5, N'Siva', CAST(5.6 AS decimal(18,2)), 8),
                    (17, N'BMW', N'1', N'VK-1717-PP', 2019, 5, N'Bijela', CAST(6.8 AS decimal(18,2)), 10),
                    (18, N'Audi', N'A3', N'PO-1818-QQ', 2020, 5, N'Crna', CAST(6.7 AS decimal(18,2)), 12),
                    (19, N'Mercedes', N'A', N'ZG-1919-RR', 2021, 5, N'Srebrna', CAST(6.9 AS decimal(18,2)), 14),
                    (20, N'Fiat', N'Tipo', N'OS-2020-SS', 2018, 5, N'Crvena', CAST(5.7 AS decimal(18,2)), 16)
                ) AS v(Id, Marka, Model, Registracija, GodinaProizvodnje, BrojSjedala, Boja, ProsjecnaPotrosnja, VlasnikId)
                WHERE NOT EXISTS (SELECT 1 FROM Vozila vo WHERE vo.Id = v.Id);
                SET IDENTITY_INSERT Vozila OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Rezervacije ON;
                INSERT INTO Rezervacije (Id, VoznjaId, PutnikId, BrojMjesta, CijenaUkupno, VrijemeRezervacije, Status, Napomena)
                SELECT n.Id, n.VoznjaId, n.PutnikId, 1, CAST(7.50 + n.Id AS decimal(18,2)), DATEADD(minute, n.Id * 7, CAST('2026-05-11T08:00:00' AS datetime2)), 0, N'Auto-seed rezervacija'
                FROM (VALUES
                    (1, 1, 2), (2, 2, 5), (3, 3, 7), (4, 4, 9), (5, 5, 11),
                    (6, 6, 13), (7, 7, 15), (8, 8, 17), (9, 9, 19), (10, 10, 21),
                    (11, 11, 23), (12, 12, 2), (13, 13, 5), (14, 14, 7), (15, 15, 9),
                    (16, 16, 11), (17, 17, 13), (18, 18, 15), (19, 19, 17), (20, 20, 19)
                ) AS n(Id, VoznjaId, PutnikId)
                INNER JOIN Voznje v ON v.Id = n.VoznjaId
                INNER JOIN Korisnici k ON k.Id = n.PutnikId
                WHERE NOT EXISTS (SELECT 1 FROM Rezervacije r WHERE r.Id = n.Id);
                SET IDENTITY_INSERT Rezervacije OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Placanja ON;
                INSERT INTO Placanja (Id, RezervacijaId, Iznos, VrijemePlacanja, NacinPlacanja, Uspjesno)
                SELECT r.Id, r.Id, r.CijenaUkupno, DATEADD(minute, 30, r.VrijemeRezervacije), 1, CAST(1 AS bit)
                FROM Rezervacije r
                WHERE r.Id BETWEEN 1 AND 20
                  AND NOT EXISTS (SELECT 1 FROM Placanja p WHERE p.Id = r.Id);
                SET IDENTITY_INSERT Placanja OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Ocjene ON;
                INSERT INTO Ocjene (Id, RezervacijaId, AutorId, BrojZvjezdica, Komentar, Kreirano)
                SELECT r.Id, r.Id, r.PutnikId, 4 + (r.Id % 2), N'Auto-seed ocjena', DATEADD(hour, 2, r.VrijemeRezervacije)
                FROM Rezervacije r
                WHERE r.Id BETWEEN 1 AND 20
                  AND NOT EXISTS (SELECT 1 FROM Ocjene o WHERE o.Id = r.Id);
                SET IDENTITY_INSERT Ocjene OFF;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
