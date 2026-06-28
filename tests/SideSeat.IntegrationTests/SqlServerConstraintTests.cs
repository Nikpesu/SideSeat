using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.IntegrationTests;

public sealed class SqlServerConstraintTests
{
    [Fact]
    public async Task MigrationsAndUniqueConstraints_WorkOnSqlServer()
    {
        var baseConnection = Environment.GetEnvironmentVariable("SQL_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var databaseName = $"SideSeatTests_{Guid.NewGuid():N}";
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = databaseName
        };
        var options = new DbContextOptionsBuilder<SideSeatDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;

        await using var db = new SideSeatDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            Assert.True(await db.Database.CanConnectAsync());
            Assert.Contains(
                await db.Database.GetAppliedMigrationsAsync(),
                migration => migration.EndsWith("_AddAuditLoggingAndDataConstraints"));
            Assert.Contains(
                await db.Database.GetAppliedMigrationsAsync(),
                migration => migration.EndsWith("_AddCityCoordinates"));
            var zagreb = await db.Gradovi.AsNoTracking().SingleAsync(city => city.Id == 1);
            Assert.Equal(45.815010m, zagreb.Latitude);
            Assert.Equal(15.981919m, zagreb.Longitude);

            var cityName = $"Constraint-{Guid.NewGuid():N}";
            db.Gradovi.Add(new Grad
            {
                Naziv = cityName,
                Drzava = "Hrvatska",
                PostanskiBroj = "10000"
            });
            await db.SaveChangesAsync();

            db.Gradovi.Add(new Grad
            {
                Naziv = cityName,
                Drzava = "Hrvatska",
                PostanskiBroj = "10001"
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            db.ChangeTracker.Clear();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
