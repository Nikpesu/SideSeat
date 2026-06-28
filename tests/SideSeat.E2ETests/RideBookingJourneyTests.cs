using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace SideSeat.E2ETests;

/// <summary>
/// End-to-end Playwright scenarij (10 koraka) koji kao prijavljeni admin prođe
/// kroz prijavu, globalnu pretragu, navigaciju i pregled administracije te se odjavi.
///
/// Konfiguracija preko varijabli okruženja (sve imaju default):
///   SIDESEAT_BASE_URL   – ciljna adresa (default: https://sideseat.pesut.win)
///   SIDESEAT_E2E_EMAIL  – email za prijavu  (default: admin@example.com)
///   SIDESEAT_E2E_PASS   – lozinka           (default: Admin123!)
/// </summary>
[TestFixture]
public sealed class RideBookingJourneyTests : PageTest
{
    private static string BaseUrl =>
        (Environment.GetEnvironmentVariable("SIDESEAT_BASE_URL") ?? "https://sideseat.pesut.win")
            .TrimEnd('/');

    private static string Email =>
        Environment.GetEnvironmentVariable("SIDESEAT_E2E_EMAIL") ?? "admin@example.com";

    private static string Password =>
        Environment.GetEnvironmentVariable("SIDESEAT_E2E_PASS") ?? "Admin123!";

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        ViewportSize = new() { Width = 1366, Height = 900 },
        IgnoreHTTPSErrors = true,
        Locale = "hr-HR"
    };

    [Test]
    public async Task Admin_prolazi_kroz_prijavu_pretragu_i_administraciju()
    {
        // --- Korak 1: otvori početnu stranicu ---
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page).ToHaveTitleAsync(new Regex("SideSeat", RegexOptions.IgnoreCase));
        TestContext.Out.WriteLine("1/10 Početna stranica učitana.");

        // --- Korak 2: otvori modal za prijavu ---
        await Page.Locator("[data-auth-open='login']").First.ClickAsync();
        var emailField = Page.Locator("#auth-login-email");
        await Expect(emailField).ToBeVisibleAsync();
        TestContext.Out.WriteLine("2/10 Login modal otvoren.");

        // --- Korak 3: unesi kredencijale i prijavi se ---
        await emailField.FillAsync(Email);
        await Page.Locator("#auth-login-password").FillAsync(Password);
        await Page.Locator("#auth-login-password").PressAsync("Enter");
        var accountLink = Page.Locator("[data-testid='account-settings']");
        await Expect(accountLink).ToBeVisibleAsync(new() { Timeout = 15_000 });
        TestContext.Out.WriteLine("3/10 Prijava uspješna.");

        // --- Korak 4: otvori globalnu pretragu ---
        await Page.Locator("[data-testid='global-search-trigger']").First.ClickAsync();
        var searchInput = Page.Locator("#ss-global-search-input");
        await Expect(searchInput).ToBeVisibleAsync();
        TestContext.Out.WriteLine("4/10 Globalna pretraga otvorena.");

        // --- Korak 5: pretraži i provjeri rezultate ---
        await searchInput.FillAsync("Postav");
        var settingsResult = Page
            .Locator("[data-ss-global-search-results] a", new() { HasTextString = "Postavke" })
            .First;
        await Expect(settingsResult).ToBeVisibleAsync(new() { Timeout = 10_000 });
        TestContext.Out.WriteLine("5/10 Pretraga vraća rezultat 'Postavke'.");

        // --- Korak 6: otvori stranicu iz rezultata pretrage ---
        await settingsResult.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Korisnik/Settings", RegexOptions.IgnoreCase));
        TestContext.Out.WriteLine("6/10 Navigacija iz pretrage na Postavke radi.");

        // --- Korak 7: otvori popis vožnji (CRUD lista) ---
        await Page.Locator("[data-testid='nav-rides']").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Voznja", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("main, .ss-shell").First).ToBeVisibleAsync();
        TestContext.Out.WriteLine("7/10 Popis vožnji učitan.");

        // --- Korak 8: otvori administraciju gradova (admin pristup) ---
        await Page.GotoAsync($"{BaseUrl}/Grad", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page).ToHaveURLAsync(new Regex("/Grad", RegexOptions.IgnoreCase));
        Assert.That(Page.Url, Does.Not.Contain("auth=login"),
            "Admin bi trebao imati pristup administraciji gradova.");
        TestContext.Out.WriteLine("8/10 Administracija gradova dostupna adminu.");

        // --- Korak 9: otvori audit zapisnik (logging mehanizam) ---
        await Page.GotoAsync($"{BaseUrl}/Audit", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page).ToHaveURLAsync(new Regex("/Audit", RegexOptions.IgnoreCase));
        Assert.That(Page.Url, Does.Not.Contain("auth=login"),
            "Admin bi trebao vidjeti audit zapisnik.");
        TestContext.Out.WriteLine("9/10 Audit zapisnik dostupan.");

        // --- Korak 10: odjava ---
        await Page.Locator("[data-testid='top-logout']").First.ClickAsync();
        await Expect(Page.Locator("[data-auth-open='login']").First).ToBeVisibleAsync(new() { Timeout = 15_000 });
        TestContext.Out.WriteLine("10/10 Odjava uspješna.");
    }
}
