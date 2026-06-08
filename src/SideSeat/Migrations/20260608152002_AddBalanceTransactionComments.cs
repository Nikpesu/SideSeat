using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceTransactionComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Komentar",
                table: "SaldoTransakcije",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Komentar",
                table: "SaldoTransakcije");
        }
    }
}
