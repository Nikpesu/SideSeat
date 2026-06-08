using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddMockPaymentPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpremljenaAdresaPlacanja",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpremljenaKarticaIme",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpremljenaKarticaVrijediDo",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpremljenaKarticaZadnjeCetiri",
                table: "Korisnici",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpremljenaAdresaPlacanja",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "SpremljenaKarticaIme",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "SpremljenaKarticaVrijediDo",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "SpremljenaKarticaZadnjeCetiri",
                table: "Korisnici");
        }
    }
}
