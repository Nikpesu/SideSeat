using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddRideWorkflowLiveFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CashCollectedAtUtc",
                table: "Rezervacije",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInAtUtc",
                table: "Rezervacije",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastLatitude",
                table: "Rezervacije",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationAtUtc",
                table: "Rezervacije",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastLongitude",
                table: "Rezervacije",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NacinPlacanja",
                table: "Rezervacije",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<decimal>(
                name: "Napojnica",
                table: "Rezervacije",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RideChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoznjaId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideChatMessages_Korisnici_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RideChatMessages_Korisnici_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RideChatMessages_Voznje_VoznjaId",
                        column: x => x.VoznjaId,
                        principalTable: "Voznje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideChatMessages_RecipientId",
                table: "RideChatMessages",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_RideChatMessages_SenderId",
                table: "RideChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_RideChatMessages_VoznjaId_CreatedAtUtc",
                table: "RideChatMessages",
                columns: new[] { "VoznjaId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideChatMessages");

            migrationBuilder.DropColumn(
                name: "CashCollectedAtUtc",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "CheckInAtUtc",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "LastLatitude",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "LastLocationAtUtc",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "LastLongitude",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "NacinPlacanja",
                table: "Rezervacije");

            migrationBuilder.DropColumn(
                name: "Napojnica",
                table: "Rezervacije");
        }
    }
}
