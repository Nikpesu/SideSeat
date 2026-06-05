using System.Net;
using System.Net.Http.Json;
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
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.GetAsync($"/api/gradovi/{dto!.id}")).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/gradovi/{dto.id}", new { naziv = "Osijek updated", drzava = "Hrvatska", postanskiBroj = "31000" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/gradovi/999", new { naziv = "X", drzava = "Y", postanskiBroj = "1" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/gradovi/{dto.id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/gradovi/{dto.id}")).StatusCode);
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

        var request = new { voznjaId = 1, putnikId = 2, brojMjesta = 1, status = StatusRezervacije.Aktivna, napomena = "API rezervacija" };
        var created = await client.PostAsJsonAsync("/api/rezervacije", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/rezervacije/{dto!.id}", new { voznjaId = 1, putnikId = 2, brojMjesta = 1, status = StatusRezervacije.Aktivna, napomena = "Updated" })).EnsureSuccessStatusCode();
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

        var request = new { rezervacijaId = 1, iznos = 20m, vrijemePlacanja = DateTime.UtcNow, nacinPlacanja = NacinPlacanja.Online, uspjesno = true };
        var created = await client.PostAsJsonAsync("/api/placanja", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<SimpleDto>();

        (await client.PutAsJsonAsync($"/api/placanja/{dto!.id}", new { rezervacijaId = 1, iznos = 20m, vrijemePlacanja = request.vrijemePlacanja, nacinPlacanja = NacinPlacanja.Online, uspjesno = false })).EnsureSuccessStatusCode();
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
    public async Task VoznjaAttachments_CoversUploadListAndDelete()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("test dokument"), "file", "test.txt");

        var upload = await client.PostAsync("/Voznja/UploadAttachment?voznjaId=1", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var list = await client.GetAsync("/Voznja/GetAttachments?voznjaId=1");
        list.EnsureSuccessStatusCode();
        Assert.Contains("test.txt", await list.Content.ReadAsStringAsync());

        int attachmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            attachmentId = await db.VoznjaAttachments.Select(a => a.Id).SingleAsync();
        }

        var delete = await client.PostAsync($"/Voznja/DeleteAttachment?id={attachmentId}", content: null);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    private sealed record SimpleDto(int id);
}
