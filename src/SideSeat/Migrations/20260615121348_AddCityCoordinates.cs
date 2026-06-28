using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddCityCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Gradovi",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Gradovi",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE g
                SET g.Latitude = coordinates.Latitude,
                    g.Longitude = coordinates.Longitude
                FROM Gradovi g
                INNER JOIN (VALUES
                    (1, CAST(45.815010 AS decimal(9,6)), CAST(15.981919 AS decimal(9,6))),
                    (2, CAST(43.508133 AS decimal(9,6)), CAST(16.440193 AS decimal(9,6))),
                    (3, CAST(45.327063 AS decimal(9,6)), CAST(14.442176 AS decimal(9,6))),
                    (4, CAST(45.555000 AS decimal(9,6)), CAST(18.695514 AS decimal(9,6))),
                    (5, CAST(44.119371 AS decimal(9,6)), CAST(15.231365 AS decimal(9,6))),
                    (6, CAST(44.866623 AS decimal(9,6)), CAST(13.849579 AS decimal(9,6))),
                    (7, CAST(45.160278 AS decimal(9,6)), CAST(18.015582 AS decimal(9,6))),
                    (8, CAST(45.492897 AS decimal(9,6)), CAST(15.555268 AS decimal(9,6))),
                    (9, CAST(46.305746 AS decimal(9,6)), CAST(16.336607 AS decimal(9,6))),
                    (10, CAST(43.735019 AS decimal(9,6)), CAST(15.889164 AS decimal(9,6))),
                    (11, CAST(42.650661 AS decimal(9,6)), CAST(18.094424 AS decimal(9,6))),
                    (12, CAST(45.487209 AS decimal(9,6)), CAST(16.375137 AS decimal(9,6))),
                    (13, CAST(45.351610 AS decimal(9,6)), CAST(19.002250 AS decimal(9,6))),
                    (14, CAST(45.898853 AS decimal(9,6)), CAST(16.842310 AS decimal(9,6))),
                    (15, CAST(46.162800 AS decimal(9,6)), CAST(16.827700 AS decimal(9,6))),
                    (16, CAST(46.384400 AS decimal(9,6)), CAST(16.433900 AS decimal(9,6))),
                    (17, CAST(45.288500 AS decimal(9,6)), CAST(18.804800 AS decimal(9,6))),
                    (18, CAST(43.296900 AS decimal(9,6)), CAST(17.017000 AS decimal(9,6))),
                    (19, CAST(45.801100 AS decimal(9,6)), CAST(15.711000 AS decimal(9,6))),
                    (20, CAST(45.713200 AS decimal(9,6)), CAST(16.075200 AS decimal(9,6))),
                    (21, CAST(43.550000 AS decimal(9,6)), CAST(16.350000 AS decimal(9,6))),
                    (22, CAST(45.340300 AS decimal(9,6)), CAST(17.685300 AS decimal(9,6)))
                ) coordinates(Id, Latitude, Longitude) ON coordinates.Id = g.Id
                WHERE g.Latitude IS NULL OR g.Longitude IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Gradovi");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Gradovi");
        }
    }
}
