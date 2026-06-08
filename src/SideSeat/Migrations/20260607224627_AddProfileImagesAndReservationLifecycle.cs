using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImagesAndReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilnaSlikaPath",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE r
                SET r.Status = 3
                FROM Rezervacije r
                INNER JOIN Voznje v ON v.Id = r.VoznjaId
                WHERE v.Status = 1 AND r.Status IN (0, 1);

                WITH MjestaZaVratiti AS
                (
                    SELECT r.VoznjaId, SUM(r.BrojMjesta) AS BrojMjesta
                    FROM Rezervacije r
                    INNER JOIN Voznje v ON v.Id = r.VoznjaId
                    WHERE v.Status = 0 AND r.Status IN (0, 2)
                    GROUP BY r.VoznjaId
                )
                UPDATE v
                SET v.SlobodnaMjesta =
                    CASE
                        WHEN v.SlobodnaMjesta + m.BrojMjesta > v.UkupnoMjesta THEN v.UkupnoMjesta
                        ELSE v.SlobodnaMjesta + m.BrojMjesta
                    END
                FROM Voznje v
                INNER JOIN MjestaZaVratiti m ON m.VoznjaId = v.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilnaSlikaPath",
                table: "Korisnici");
        }
    }
}
