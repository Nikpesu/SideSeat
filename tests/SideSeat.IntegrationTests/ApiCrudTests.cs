using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.IntegrationTests;

public class ApiCrudTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public ApiCrudTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GradoviApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/gradovi")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/gradovi/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/gradovi", new { })).StatusCode);

        var created = await client.PostAsJsonAsync("/api/gradovi", new { naziv = "Osijek", drzava = "Hrvatska", postanskiBroj = "31000" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Contains("\"latitude\":45.123456", await created.Content.ReadAsStringAsync());
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.GetAsync($"/api/gradovi/{dto!.id}")).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/gradovi/{dto.id}", new { naziv = "Osijek updated", drzava = "Hrvatska", postanskiBroj = "31000" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/gradovi/999", new { naziv = "X", drzava = "Y", postanskiBroj = "1" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/gradovi/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/gradovi/{dto.id}")).StatusCode);

        var geocodingFailure = await client.PostAsJsonAsync(
            "/api/gradovi",
            new { naziv = "Bez Lokacije", drzava = "Hrvatska", postanskiBroj = "99999" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, geocodingFailure.StatusCode);
        Assert.Contains("Lokacija grada nije pronađena", await geocodingFailure.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task KorisniciApi_CoversCrudAndAuthorization()
    {
        await _factory.SeedAsync();
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/korisnici", new { })).StatusCode);

        using var client = _factory.CreateAdminClient();
        (await client.GetAsync("/api/korisnici")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/korisnici/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/korisnici", new { })).StatusCode);

        var request = new { ime = "Ana", prezime = "Api", email = "ana.api@example.com", adresa = "Test 1", brojMobitela = "0911234567", tip = TipKorisnika.Putnik, jeAktivan = true };
        var created = await client.PostAsJsonAsync("/api/korisnici", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.GetAsync($"/api/korisnici/{dto!.id}")).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/korisnici/{dto.id}", new { ime = "Ana", prezime = "Updated", email = "ana.api@example.com", adresa = "Test 1", brojMobitela = "0911234567", tip = TipKorisnika.Putnik, jeAktivan = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/korisnici/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/korisnici/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task KorisnikDetails_ShowsLimitedProfileToOtherUsers()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();
        using var admin = _factory.CreateAdminClient();

        Assert.Equal(HttpStatusCode.OK, (await passenger.GetAsync("/Korisnik/Details/2")).StatusCode);
        var limitedProfile = await passenger.GetStringAsync("/Korisnik/Details/1");
        Assert.Contains("Admin User", limitedProfile);
        Assert.Contains("0910000001", limitedProfile);
        Assert.Contains("/images/profile-placeholder.svg", limitedProfile);
        Assert.DoesNotContain("<dt>Email</dt>", limitedProfile);
        Assert.DoesNotContain("Admin adresa", limitedProfile);
        Assert.DoesNotContain("KYC status", limitedProfile);
        Assert.Equal(HttpStatusCode.OK, (await passenger.GetAsync("/api/korisnici/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/korisnici/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Korisnik/Details/2")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/korisnici/2")).StatusCode);
    }

    [Fact]
    public async Task VoznjaTabs_ShowAdminViewsOnlyToAdmin()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();
        using var driver = _factory.CreateDriverClient();
        using var admin = _factory.CreateAdminClient();

        var passengerHtml = await passenger.GetStringAsync("/Voznja?view=available");
        Assert.Contains("id=\"ss-theme-toggle\"", passengerHtml);
        Assert.Contains("localStorage.getItem(\"ss-theme\")", passengerHtml);
        Assert.Contains("/css/performative.css", passengerHtml);
        Assert.Contains("data-ss-ai", passengerHtml);
        Assert.Contains("SideSeat Copilot", passengerHtml);
        Assert.Contains("data-configured=\"false\"", passengerHtml);
        Assert.Contains("class=\"ss-route-bg\"", passengerHtml);
        Assert.Contains("data-ss-route-bg", passengerHtml);
        Assert.Contains("Dostupne vožnje", passengerHtml);
        Assert.DoesNotContain("Moje vožnje", passengerHtml);
        Assert.Contains("Moja voženja", passengerHtml);
        Assert.DoesNotContain("view=all", passengerHtml);
        Assert.Contains("Status voznji", passengerHtml);
        Assert.Contains("ss-selector-count\">1</span>", passengerHtml);
        Assert.Contains("ss-selector-count\">0</span>", passengerHtml);
        Assert.Contains("status=planned", passengerHtml);
        Assert.Contains("status=completed", passengerHtml);
        Assert.Contains("status=cancelled", passengerHtml);

        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/Voznja?view=all")).StatusCode);

        var adminHtml = await admin.GetStringAsync("/Voznja?view=all");
        Assert.Contains("Sve voznje", adminHtml);
        Assert.Contains("Dostupne vožnje", adminHtml);
        Assert.Contains("Moje vožnje", adminHtml);
        Assert.Contains("Moja voženja", adminHtml);
        Assert.Contains("status=planned", adminHtml);
        Assert.Contains("status=completed", adminHtml);
        Assert.Contains("status=cancelled", adminHtml);
        Assert.Contains(">Rezervacije<", adminHtml);

        var driverHtml = await driver.GetStringAsync("/Voznja?view=driving");
        Assert.Contains("Moje vožnje", driverHtml);

        var activeHtml = await passenger.GetStringAsync("/Voznja?view=available&status=planned");
        Assert.Contains("Zagreb", activeHtml);
        var riddenHtml = await passenger.GetStringAsync("/Voznja?view=ridden&status=all");
        Assert.Contains("Zagreb", riddenHtml);
        var drivingHtml = await passenger.GetStringAsync("/Voznja?view=driving&status=all");
        Assert.DoesNotContain("Zagreb -&gt; Split", drivingHtml);
        var completedHtml = await admin.GetStringAsync("/Voznja?view=all&status=completed");
        Assert.DoesNotContain("Zagreb -&gt; Split", completedHtml);
    }

    [Fact]
    public async Task RezervacijaTabs_RespectAdminPassengerAndDriverViews()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();
        using var driver = _factory.CreateDriverClient();
        using var admin = _factory.CreateAdminClient();

        var passengerHtml = await passenger.GetStringAsync("/Rezervacija?view=mine");
        Assert.Contains("Moje rezervacije", passengerHtml);
        Assert.Contains("Vožnja #1", passengerHtml);
        Assert.Contains("/Voznja/Details/1", passengerHtml);
        Assert.DoesNotContain("Sve rezervacije", passengerHtml);
        Assert.DoesNotContain("Rezervacije mojih voznji", passengerHtml);
        Assert.Contains("Status rezervacija", passengerHtml);
        Assert.Contains("ss-selector-count\">1</span>", passengerHtml);
        Assert.Contains("ss-selector-count\">0</span>", passengerHtml);
        Assert.Contains("status=pending", passengerHtml);
        Assert.Contains("status=confirmed", passengerHtml);
        Assert.Contains("status=rejected", passengerHtml);
        Assert.Contains("status=completed", passengerHtml);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/Rezervacija?view=all")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/Rezervacija?view=my-rides")).StatusCode);

        var driverHtml = await driver.GetStringAsync("/Rezervacija?view=my-rides");
        Assert.Contains("Rezervacije mojih voznji", driverHtml);
        Assert.Contains("Vožnja #1", driverHtml);
        Assert.DoesNotContain("view=all", driverHtml);

        var adminHtml = await admin.GetStringAsync("/Rezervacija?view=all");
        Assert.Contains("Sve rezervacije", adminHtml);
        Assert.Contains("Rezervacije mojih voznji", adminHtml);
        Assert.Contains("view=all", adminHtml);
        Assert.Contains("status=pending", adminHtml);
        Assert.Contains("status=confirmed", adminHtml);
        Assert.Contains("status=rejected", adminHtml);
        Assert.Contains("status=completed", adminHtml);

        var completedHtml = await admin.GetStringAsync("/Rezervacija?view=all&status=completed");
        Assert.DoesNotContain("Vožnja #1", completedHtml);
    }

    [Fact]
    public async Task ReservationRequiresFundsAndMockTopUpAddsBalance()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var passengerUser = await db.Korisnici.SingleAsync(k => k.Id == 2);
            passengerUser.Saldo = 5;
            db.Ocjene.RemoveRange(db.Ocjene);
            db.Placanja.RemoveRange(db.Placanja);
            db.Rezervacije.RemoveRange(db.Rezervacije);
            await db.SaveChangesAsync();
        }

        var createPage = await passenger.GetStringAsync("/Rezervacija/Create?voznjaId=1");
        var token = ExtractAntiforgeryToken(createPage);
        using var insufficientForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["VoznjaId"] = "1",
            ["BrojMjesta"] = "1",
            ["Napomena"] = "Treba saldo"
        });
        var insufficientResponse = await passenger.PostAsync("/Rezervacija/Create", insufficientForm);
        var insufficientHtml = await insufficientResponse.Content.ReadAsStringAsync();

        Assert.Contains("Nedovoljno sredstava", insufficientHtml);
        Assert.Contains("Uplati sredstva", insufficientHtml);
        Assert.Contains("15,00 EUR", insufficientHtml);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Empty(await db.Rezervacije.ToListAsync());
        }

        var topUpPage = await passenger.GetStringAsync("/Korisnik/Uplata?amount=15&returnUrl=/Rezervacija/Create%3FvoznjaId%3D1");
        Assert.Contains("Mock checkout", topUpPage);
        Assert.Contains("Revolut Pay", topUpPage);
        Assert.Contains("Otvoren vanjski servis", topUpPage);
        Assert.Contains("maxlength=\"19\"", topUpPage);
        Assert.Contains("maxlength=\"5\"", topUpPage);
        Assert.Contains("maxlength=\"4\"", topUpPage);
        Assert.Contains("Verzija v0.45", topUpPage);
        var topUpToken = ExtractAntiforgeryToken(topUpPage);
        using var topUpForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = topUpToken,
            ["Iznos"] = "15",
            ["NacinPlacanja"] = "Kartica",
            ["CardholderName"] = "Putnik Test",
            ["CardNumber"] = "4242 4242 4242 4242",
            ["CardExpiry"] = "12/30",
            ["CardCvv"] = "123",
            ["SaveCard"] = "true",
            ["BillingStreet"] = "Test adresa",
            ["BillingHouseNumber"] = "15",
            ["BillingPostalCode"] = "10000",
            ["BillingCountry"] = "Hrvatska",
            ["SaveBillingAddress"] = "true",
            ["ReturnUrl"] = "/Rezervacija/Create?voznjaId=1"
        });
        var topUpResponse = await passenger.PostAsync("/Korisnik/Uplata", topUpForm);
        var topUpHtml = await topUpResponse.Content.ReadAsStringAsync();
        Assert.Contains("Rezervacija voznje", topUpHtml);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var korisnik = await db.Korisnici.SingleAsync(k => k.Id == 2);
            Assert.Equal(20, korisnik.Saldo);
            Assert.Equal("4242", korisnik.SpremljenaKarticaZadnjeCetiri);
            Assert.Equal("Test adresa 15, 10000, Hrvatska", korisnik.SpremljenaAdresaPlacanja);
            Assert.Contains(await db.SaldoTransakcije.ToListAsync(), t =>
                t.KorisnikId == 2 &&
                t.Tip == "uplata-kartica" &&
                t.Iznos == 15 &&
                t.Komentar == "Uplaćeno sa *4242 kartice");
        }

        var fundedToken = ExtractAntiforgeryToken(topUpHtml);
        using var fundedForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = fundedToken,
            ["VoznjaId"] = "1",
            ["BrojMjesta"] = "1",
            ["Napomena"] = "Sada ima saldo"
        });
        await passenger.PostAsync("/Rezervacija/Create", fundedForm);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Single(await db.Rezervacije.ToListAsync());
        }
    }

    [Fact]
    public async Task PayPalAndRevolutMockPaymentsCreateTransactions()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();

        foreach (var payment in new[]
                 {
                     new { Method = "PayPal", Account = "putnik@paypal.test", Provider = "PayPal", Tip = "uplata-paypal" },
                     new { Method = "Revolut Pay", Account = "@putnik-revolut", Provider = "Revolut", Tip = "uplata-revolut-pay" }
                 })
        {
            var page = await passenger.GetStringAsync("/Korisnik/Uplata?amount=10");
            var token = ExtractAntiforgeryToken(page);
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Iznos"] = "10",
                ["NacinPlacanja"] = payment.Method,
                ["ExternalAccountName"] = payment.Account,
                ["ExternalPaymentConfirmed"] = "true",
                ["BillingStreet"] = "Vanjska ulica",
                ["BillingHouseNumber"] = "8",
                ["BillingPostalCode"] = "10000",
                ["BillingCountry"] = "Hrvatska",
                ["ReturnUrl"] = "/Korisnik/Saldo"
            });

            var response = await passenger.PostAsync("/Korisnik/Uplata", form);
            Assert.True(response.IsSuccessStatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Contains(await db.SaldoTransakcije.ToListAsync(), transaction =>
                transaction.KorisnikId == 2 &&
                transaction.Tip == payment.Tip &&
                transaction.Komentar == $"Uplaćeno sa {payment.Account} {payment.Provider} računa");
        }

        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        Assert.Equal(70, (await finalDb.Korisnici.SingleAsync(k => k.Id == 2)).Saldo);

        var saldoPage = await passenger.GetStringAsync("/Korisnik/Saldo");
        Assert.Contains("PayPal", saldoPage);
        Assert.Contains("Revolut Pay", saldoPage);
    }

    [Fact]
    public async Task DriverMustResolveReservationBeforeCompletingRide()
    {
        await _factory.SeedAsync();
        using var driver = _factory.CreateDriverClient();

        var ridePage = await driver.GetStringAsync("/Voznja/Details/1");
        Assert.Contains("Potvrdi", ridePage);
        Assert.Contains("Odbij", ridePage);
        Assert.Contains("/Rezervacija/Confirm", ridePage);
        Assert.Contains("/Rezervacija/Reject", ridePage);
        var rideToken = ExtractAntiforgeryToken(ridePage);
        using (var executeForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = rideToken
        }))
        {
            await driver.PostAsync("/Voznja/Izvrsi/1", executeForm);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Equal(StatusVoznje.Planirana, (await db.Voznje.SingleAsync(v => v.Id == 1)).Status);
            Assert.Equal(StatusRezervacije.UProcesuPotvrde, (await db.Rezervacije.SingleAsync(r => r.Id == 1)).Status);
        }

        var reservationPage = await driver.GetStringAsync("/Rezervacija/Details/1");
        var reservationToken = ExtractAntiforgeryToken(reservationPage);
        using (var confirmForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = reservationToken,
            ["id"] = "1"
        }))
        {
            await driver.PostAsync("/Rezervacija/Confirm", confirmForm);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Equal(StatusRezervacije.Potvrdena, (await db.Rezervacije.SingleAsync(r => r.Id == 1)).Status);
            Assert.Equal(3, (await db.Voznje.SingleAsync(v => v.Id == 1)).SlobodnaMjesta);
            Assert.Contains(await db.Obavijesti.ToListAsync(), o =>
                o.KorisnikId == 2 && o.Naslov == "Rezervacija potvrđena" && !o.Procitano);
        }

        using (var passenger = _factory.CreatePassengerClient())
        {
            var passengerHtml = await passenger.GetStringAsync("/");
            Assert.Contains("ss-notification-count", passengerHtml);
            Assert.Contains("Rezervacija potvr", passengerHtml);

            int notificationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
                notificationId = await db.Obavijesti
                    .Where(o => o.KorisnikId == 2 && !o.Procitano)
                    .Select(o => o.Id)
                    .FirstAsync();
            }

            await passenger.GetAsync($"/Obavijest/Open/{notificationId}");
            using var readScope = _factory.Services.CreateScope();
            var readDb = readScope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.True((await readDb.Obavijesti.SingleAsync(o => o.Id == notificationId)).Procitano);
        }

        ridePage = await driver.GetStringAsync("/Voznja/Details/1");
        rideToken = ExtractAntiforgeryToken(ridePage);
        using (var executeForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = rideToken
        }))
        {
            await driver.PostAsync("/Voznja/Izvrsi/1", executeForm);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            Assert.Equal(StatusVoznje.Zavrsena, (await db.Voznje.SingleAsync(v => v.Id == 1)).Status);
            Assert.Equal(StatusRezervacije.Zavrsena, (await db.Rezervacije.SingleAsync(r => r.Id == 1)).Status);
        }
    }

    [Fact]
    public async Task UserCanUploadProfileImage()
    {
        await _factory.SeedAsync();
        using var passenger = _factory.CreatePassengerClient();

        var settingsPage = await passenger.GetStringAsync("/Korisnik/Settings");
        var antiforgeryToken = ExtractAntiforgeryToken(settingsPage);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(antiforgeryToken), "__RequestVerificationToken");
        form.Add(new StringContent("Nova adresa"), "Address");
        form.Add(new StringContent("0911111111"), "PhoneNumber");
        form.Add(new StringContent("true"), "IsRider");
        var imageContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "ProfileImage", "profil.png");

        var response = await passenger.PostAsync("/Korisnik/Settings", form);
        Assert.True(response.IsSuccessStatusCode);

        string imagePath;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var korisnik = await db.Korisnici.SingleAsync(k => k.Id == 2);
            Assert.StartsWith("/uploads/profili/2/", korisnik.ProfilnaSlikaPath);
            imagePath = korisnik.ProfilnaSlikaPath!;
            Assert.True(File.Exists(Path.Combine(
                _factory.WebRootPath,
                imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }

        var imageResponse = await passenger.GetAsync(imagePath);
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        var profileHtml = await passenger.GetStringAsync("/Korisnik/Details/2");
        Assert.Contains(imagePath, profileHtml);
    }

    [Fact]
    public async Task Login_RememberMeTrue_BindsAsBoolean()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();

        var loginPage = await client.GetStringAsync("/");
        var antiforgeryToken = Regex.Match(
            loginPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgeryToken,
            ["Email"] = "missing@example.com",
            ["Password"] = "Password123!",
            ["RememberMe"] = "true"
        });

        var response = await client.PostAsync("/Auth/Login", form);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Neispravan email ili lozinka.", html);
        Assert.DoesNotContain("Provjeri unesene podatke.", html);
    }

    [Fact]
    public async Task VozilaApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/vozila")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vozila/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/vozila", new { })).StatusCode);

        var request = new { marka = "VW", model = "Golf", registracija = "ZG-API-1", godinaProizvodnje = 2022, brojSjedala = 5, boja = "Plava", prosjecnaPotrosnja = 5.5m };
        var created = await client.PostAsJsonAsync("/api/vozila", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/vozila/{dto!.id}", new { marka = "VW", model = "Golf", registracija = "ZG-API-1", godinaProizvodnje = 2022, brojSjedala = 5, boja = "Crna", prosjecnaPotrosnja = 5.5m })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/vozila/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/vozila/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/vozila/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task VoznjeApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/voznje?q=Seed")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/voznje/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/voznje", new { })).StatusCode);

        var request = new
        {
            vozacId = 1, polazniGradId = 1, odredisniGradId = 3,
            polazak = DateTime.UtcNow.AddDays(3), ocekivaniDolazak = DateTime.UtcNow.AddDays(3).AddHours(3),
            cijenaPoMjestu = 15m, ukupnoMjesta = 4, slobodnaMjesta = 4, opis = "API voznja", status = StatusVoznje.Planirana
        };
        var created = await client.PostAsJsonAsync("/api/voznje", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/voznje/{dto!.id}", new
        {
            vozacId = 1, polazniGradId = 1, odredisniGradId = 3,
            polazak = request.polazak, ocekivaniDolazak = request.ocekivaniDolazak,
            cijenaPoMjestu = 15m, ukupnoMjesta = 4, slobodnaMjesta = 4, opis = "Updated", status = StatusVoznje.Planirana
        })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/voznje/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/voznje/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/voznje/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task RezervacijeApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/rezervacije")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/rezervacije/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rezervacije", new { })).StatusCode);

        var request = new { voznjaId = 1, putnikId = 2, brojMjesta = 1, status = StatusRezervacije.UProcesuPotvrde, napomena = "API rezervacija" };
        var created = await client.PostAsJsonAsync("/api/rezervacije", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/rezervacije/{dto!.id}", new { voznjaId = 1, putnikId = 2, brojMjesta = 1, status = StatusRezervacije.UProcesuPotvrde, napomena = "Updated" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/rezervacije/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/rezervacije/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/rezervacije/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task PlacanjaApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/placanja")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/placanja/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/placanja", new { })).StatusCode);

        var request = new { rezervacijaId = 1, iznos = 20m, vrijemePlacanja = DateTime.UtcNow, nacinPlacanja = NacinPlacanja.RevolutPay, uspjesno = true };
        var created = await client.PostAsJsonAsync("/api/placanja", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/placanja/{dto!.id}", new { rezervacijaId = 1, iznos = 20m, vrijemePlacanja = request.vrijemePlacanja, nacinPlacanja = NacinPlacanja.RevolutPay, uspjesno = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/placanja/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/placanja/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/placanja/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task OcjeneApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/ocjene")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/ocjene/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/ocjene", new { })).StatusCode);

        var request = new { rezervacijaId = 1, autorId = 2, brojZvjezdica = 4, komentar = "API ocjena" };
        var created = await client.PostAsJsonAsync("/api/ocjene", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/ocjene/{dto!.id}", new { rezervacijaId = 1, autorId = 2, brojZvjezdica = 4, komentar = "Updated" })).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync($"/api/ocjene/{dto.id}/admin-feedback", new { feedback = "" })).StatusCode);
        var feedbackResponse = await client.PutAsJsonAsync(
            $"/api/ocjene/{dto.id}/admin-feedback",
            new { feedback = "API administratorski feedback" });
        feedbackResponse.EnsureSuccessStatusCode();
        Assert.Contains("API administratorski feedback", await feedbackResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/ocjene/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/ocjene/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/ocjene/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task SaldoTransakcijeApi_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        (await client.GetAsync("/api/saldo-transakcije")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/saldo-transakcije/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/saldo-transakcije", new { })).StatusCode);

        var request = new { korisnikId = 1, iznos = 12m, tip = "api-uplata" };
        var created = await client.PostAsJsonAsync("/api/saldo-transakcije", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/saldo-transakcije/{dto!.id}", new { korisnikId = 1, iznos = 12m, tip = "api-update" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/saldo-transakcije/999", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/saldo-transakcije/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/saldo-transakcije/{dto.id}")).StatusCode);
    }

    [Fact]
    public async Task OcjenaImages_CoversCreateUploadAndApiRendering()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var voznja = await db.Voznje.SingleAsync(v => v.Id == 1);
            voznja.Status = StatusVoznje.Zavrsena;
            var rezervacija = await db.Rezervacije.SingleAsync(r => r.Id == 1);
            rezervacija.Status = StatusRezervacije.Zavrsena;
            await db.SaveChangesAsync();
        }

        var createForm = await client.GetStringAsync("/Ocjena/Create?rezervacijaId=1");
        Assert.Contains("Odaberi slike", createForm);
        var antiforgeryToken = Regex.Match(
            createForm,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(antiforgeryToken), "__RequestVerificationToken");
        form.Add(new StringContent("1"), "RezervacijaId");
        form.Add(new StringContent("5"), "BrojZvjezdica");
        form.Add(new StringContent("Ocjena sa slikom"), "Komentar");
        var imageContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "Slike", "recenzija.png");

        var created = await client.PostAsync("/Ocjena/Create", form);
        Assert.True(created.IsSuccessStatusCode);

        string imagePath;
        int imageId;
        int reviewId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var slika = await db.OcjenaSlike.SingleAsync(s => s.FileName == "recenzija.png");
            Assert.StartsWith("/uploads/ocjene/", slika.FilePath);
            Assert.True(File.Exists(Path.Combine(_factory.WebRootPath, slika.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
            imagePath = slika.FilePath;
            imageId = slika.Id;
            reviewId = slika.OcjenaVoznjeId;
        }

        var imageResponse = await client.GetAsync(imagePath);
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType?.MediaType);

        var api = await client.GetStringAsync("/api/ocjene");
        Assert.Contains("recenzija.png", api);
        Assert.Contains("/uploads/ocjene/", api);

        var editPage = await client.GetStringAsync($"/Ocjena/Edit/{reviewId}");
        Assert.Contains("Uredi recenziju", editPage);
        var editToken = ExtractAntiforgeryToken(editPage);
        using (var editForm = new FormUrlEncodedContent(new Dictionary<string, string>
               {
                   ["__RequestVerificationToken"] = editToken,
                   ["Id"] = reviewId.ToString(),
                   ["RezervacijaId"] = "1",
                   ["BrojZvjezdica"] = "4",
                   ["Komentar"] = "Uređena recenzija"
               }))
        {
            var edited = await client.PostAsync($"/Ocjena/Edit/{reviewId}", editForm);
            Assert.True(edited.IsSuccessStatusCode);
        }

        var additionalPage = await client.GetStringAsync("/Ocjena/Create?rezervacijaId=1");
        Assert.Contains("Dodaj dodatnu recenziju", additionalPage);
        using (var additionalForm = new FormUrlEncodedContent(new Dictionary<string, string>
               {
                   ["__RequestVerificationToken"] = ExtractAntiforgeryToken(additionalPage),
                   ["RezervacijaId"] = "1",
                   ["IsAdditional"] = "true",
                   ["BrojZvjezdica"] = "5",
                   ["Komentar"] = "Dodatna recenzija"
               }))
        {
            var additional = await client.PostAsync("/Ocjena/Create", additionalForm);
            Assert.True(additional.IsSuccessStatusCode);
        }

        var adminFeedbackPage = await client.GetStringAsync($"/Ocjena/AdminFeedback/{reviewId}");
        Assert.Contains("Komentar korisnika", adminFeedbackPage);
        using (var adminFeedbackForm = new FormUrlEncodedContent(new Dictionary<string, string>
               {
                   ["__RequestVerificationToken"] = ExtractAntiforgeryToken(adminFeedbackPage),
                   ["OcjenaId"] = reviewId.ToString(),
                   ["Feedback"] = "Administratorski odgovor na recenziju"
               }))
        {
            var feedbackSaved = await client.PostAsync($"/Ocjena/AdminFeedback/{reviewId}", adminFeedbackForm);
            Assert.True(feedbackSaved.IsSuccessStatusCode);
        }

        var reviewDetails = await client.GetStringAsync($"/Ocjena/Details/{reviewId}");
        Assert.Contains("Feedback administratora", reviewDetails);
        Assert.Contains("Administratorski odgovor na recenziju", reviewDetails);

        var attachmentList = await client.GetStringAsync("/Ocjena/AttachmentList");
        Assert.Contains("recenzija.png", attachmentList);

        using (var deleteRequest = new HttpRequestMessage(HttpMethod.Post, $"/Ocjena/DeleteImage/{imageId}"))
        {
            deleteRequest.Headers.Add("RequestVerificationToken", editToken);
            var deleted = await client.SendAsync(deleteRequest);
            deleted.EnsureSuccessStatusCode();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var editedReview = await db.Ocjene.SingleAsync(o => o.Id == reviewId);
            Assert.Equal(4, editedReview.BrojZvjezdica);
            Assert.NotNull(editedReview.Uredeno);
            Assert.Equal(2, await db.Ocjene.CountAsync(o => o.RezervacijaId == 1 && o.AutorId == 1));
            Assert.Equal("Administratorski odgovor na recenziju", editedReview.AdminFeedback);
            Assert.Equal(1, editedReview.AdminFeedbackAutorId);
            Assert.NotNull(editedReview.AdminFeedbackAt);
            Assert.False(await db.OcjenaSlike.AnyAsync(s => s.Id == imageId));
            Assert.False(File.Exists(Path.Combine(_factory.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
    }

    [Fact]
    public async Task DummyDataCleaner_RemovesMigrationSeedOnFirstDockerStart()
    {
        await _factory.SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();

        await DummyDataCleaner.RemoveAsync(db);

        Assert.Empty(await db.Korisnici.ToListAsync());
        Assert.Empty(await db.Gradovi.ToListAsync());
        Assert.Empty(await db.Voznje.ToListAsync());
        Assert.Empty(await db.Rezervacije.ToListAsync());
    }

    private sealed record SimpleDto(int id);

    private static string ExtractAntiforgeryToken(string html) =>
        Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
}
