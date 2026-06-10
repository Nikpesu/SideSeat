using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.IntegrationTests;

public class MvcCrudTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public MvcCrudTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GradoviMvc_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient(allowAutoRedirect: false);

        var index = await client.GetStringAsync("/Grad");
        Assert.Contains("Gradovi", index);

        var createPage = await client.GetStringAsync("/Grad/Create");
        var token = ExtractAntiforgeryToken(createPage);

        using var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Naziv"] = "TestGrad",
            ["Drzava"] = "TestDrzava",
            ["PostanskiBroj"] = "12345"
        });
        var createResponse = await client.PostAsync("/Grad/Create", createForm);
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var newGrad = await db.Gradovi.SingleAsync(g => g.Naziv == "TestGrad");
        Assert.Equal("TestDrzava", newGrad.Drzava);

        var editPage = await client.GetStringAsync($"/Grad/Edit/{newGrad.Id}");
        var editToken = ExtractAntiforgeryToken(editPage);

        using var editForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = editToken,
            ["Id"] = newGrad.Id.ToString(),
            ["Naziv"] = "TestGradUpdated",
            ["Drzava"] = "TestDrzava",
            ["PostanskiBroj"] = "12345"
        });
        var editResponse = await client.PostAsync($"/Grad/Edit/{newGrad.Id}", editForm);
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);

        var deletePage = await client.GetStringAsync($"/Grad/Delete/{newGrad.Id}");
        var deleteToken = ExtractAntiforgeryToken(deletePage);

        using var deleteForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken,
            ["Id"] = newGrad.Id.ToString()
        });
        var deleteResponse = await client.PostAsync($"/Grad/Delete/{newGrad.Id}", deleteForm);
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        Assert.False(await db.Gradovi.AnyAsync(g => g.Id == newGrad.Id));
    }

    [Fact]
    public async Task VozilaMvc_CoversCrudAndValidation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient(allowAutoRedirect: false);

        var index = await client.GetStringAsync("/Vozilo");
        Assert.Contains("Vozila", index);

        var createPage = await client.GetStringAsync("/Vozilo/Create");
        var token = ExtractAntiforgeryToken(createPage);

        using var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Marka"] = "TestMarka",
            ["Model"] = "TestModel",
            ["Registracija"] = "ZG-TEST-1",
            ["GodinaProizvodnje"] = "2022",
            ["VlasnikId"] = "1",
            ["BrojSjedala"] = "5",
            ["Boja"] = "TestBoja",
            ["ProsjecnaPotrosnja"] = "5,5"
        });
        var createResponse = await client.PostAsync("/Vozilo/Create", createForm);
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var newVozilo = await db.Vozila.SingleAsync(v => v.Registracija == "ZG-TEST-1");

        var editPage = await client.GetStringAsync($"/Vozilo/Edit/{newVozilo.Id}");
        var editToken = ExtractAntiforgeryToken(editPage);

        using var editForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = editToken,
            ["Id"] = newVozilo.Id.ToString(),
            ["Marka"] = "TestMarkaUpdated",
            ["Model"] = "TestModel",
            ["Registracija"] = "ZG-TEST-1",
            ["GodinaProizvodnje"] = "2022",
            ["VlasnikId"] = "1",
            ["BrojSjedala"] = "5",
            ["Boja"] = "TestBoja",
            ["ProsjecnaPotrosnja"] = "5,5"
        });
        var editResponse = await client.PostAsync($"/Vozilo/Edit/{newVozilo.Id}", editForm);
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);

        var deletePage = await client.GetStringAsync($"/Vozilo/Delete/{newVozilo.Id}");
        var deleteToken = ExtractAntiforgeryToken(deletePage);

        using var deleteForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken,
            ["Id"] = newVozilo.Id.ToString()
        });
        var deleteResponse = await client.PostAsync($"/Vozilo/Delete/{newVozilo.Id}", deleteForm);
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        Assert.False(await db.Vozila.AnyAsync(v => v.Id == newVozilo.Id));
    }

    private static string ExtractAntiforgeryToken(string html) =>
        Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
}
