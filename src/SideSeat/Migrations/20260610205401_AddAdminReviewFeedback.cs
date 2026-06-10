using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideSeat.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminReviewFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminFeedback",
                table: "Ocjene",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminFeedbackAt",
                table: "Ocjene",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminFeedbackAutorId",
                table: "Ocjene",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE targetReview
                SET
                    targetReview.AdminFeedback = adminReview.Komentar,
                    targetReview.AdminFeedbackAt = adminReview.Kreirano,
                    targetReview.AdminFeedbackAutorId = adminReview.AutorId
                FROM Ocjene AS targetReview
                CROSS APPLY
                (
                    SELECT TOP (1)
                        sourceReview.Komentar,
                        sourceReview.Kreirano,
                        sourceReview.AutorId
                    FROM Ocjene AS sourceReview
                    WHERE sourceReview.RezervacijaId = targetReview.RezervacijaId
                      AND sourceReview.Administratorska = CAST(1 AS bit)
                    ORDER BY sourceReview.Kreirano DESC, sourceReview.Id DESC
                ) AS adminReview
                WHERE targetReview.Administratorska = CAST(0 AS bit)
                  AND targetReview.Id =
                  (
                      SELECT TOP (1) regularReview.Id
                      FROM Ocjene AS regularReview
                      WHERE regularReview.RezervacijaId = targetReview.RezervacijaId
                        AND regularReview.Administratorska = CAST(0 AS bit)
                      ORDER BY regularReview.Kreirano DESC, regularReview.Id DESC
                  );

                DELETE FROM Ocjene
                WHERE Administratorska = CAST(1 AS bit);
                """);

            migrationBuilder.DropColumn(
                name: "Administratorska",
                table: "Ocjene");

            migrationBuilder.CreateIndex(
                name: "IX_Ocjene_AdminFeedbackAutorId",
                table: "Ocjene",
                column: "AdminFeedbackAutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ocjene_Korisnici_AdminFeedbackAutorId",
                table: "Ocjene",
                column: "AdminFeedbackAutorId",
                principalTable: "Korisnici",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ocjene_Korisnici_AdminFeedbackAutorId",
                table: "Ocjene");

            migrationBuilder.DropIndex(
                name: "IX_Ocjene_AdminFeedbackAutorId",
                table: "Ocjene");

            migrationBuilder.DropColumn(
                name: "AdminFeedback",
                table: "Ocjene");

            migrationBuilder.DropColumn(
                name: "AdminFeedbackAt",
                table: "Ocjene");

            migrationBuilder.DropColumn(
                name: "AdminFeedbackAutorId",
                table: "Ocjene");

            migrationBuilder.AddColumn<bool>(
                name: "Administratorska",
                table: "Ocjene",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
