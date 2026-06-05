using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideSeat.Models;

namespace SideSeat.Data;

public static class IdentityDataSeeder
{
    public const string AdminRole = "Admin";
    public const string DriverRole = "Driver";
    public const string PassengerRole = "Passenger";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = serviceProvider.GetRequiredService<SideSeatDbContext>();

        foreach (var role in new[] { AdminRole, DriverRole, PassengerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        var korisnici = await db.Korisnici.AsNoTracking().ToListAsync();
        foreach (var korisnik in korisnici)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.KorisnikId == korisnik.Id);
            if (existing is null)
            {
                existing = new AppUser
                {
                    UserName = korisnik.Email,
                    Email = korisnik.Email,
                    EmailConfirmed = true,
                    PhoneNumber = korisnik.BrojMobitela,
                    PhoneNumberConfirmed = true,
                    OIB = BuildOib(korisnik.Id),
                    JMBG = BuildJmbg(korisnik.Id),
                    KorisnikId = korisnik.Id
                };

                var password = korisnik.Tip == TipKorisnika.Admin ? "Admin123!" : "User123!";
                var createResult = await userManager.CreateAsync(existing, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Identity seed failed: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }
            }

            foreach (var role in ResolveRoles(korisnik.Tip))
            {
                if (!await userManager.IsInRoleAsync(existing, role))
                {
                    await userManager.AddToRoleAsync(existing, role);
                }
            }
        }
    }

    private static IEnumerable<string> ResolveRoles(TipKorisnika tip)
    {
        if (tip == TipKorisnika.Admin)
        {
            yield return AdminRole;
            yield break;
        }

        if (tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik)
        {
            yield return DriverRole;
        }

        if (tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik)
        {
            yield return PassengerRole;
        }
    }

    private static string BuildOib(int id) => (10000000000L + id).ToString();

    private static string BuildJmbg(int id) => (1000000000000L + id).ToString();
}
