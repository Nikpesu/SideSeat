using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Commands;
using SideSeat.Mcp;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public sealed class SeminarFeatureTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public SeminarFeatureTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_HidesAdminDataFromPassenger()
    {
        await _factory.SeedAsync();

        using var passenger = _factory.CreatePassengerClient();
        var passengerJson = await passenger.GetStringAsync("/api/search?q=Putnik");
        Assert.DoesNotContain("\"group\":\"Korisnici\"", passengerJson);
        Assert.DoesNotContain("putnik@example.com", passengerJson);

        using var admin = _factory.CreateAdminClient();
        var adminJson = await admin.GetStringAsync("/api/search?q=Putnik");
        Assert.Contains("\"group\":\"Korisnici\"", adminJson);
        Assert.Contains("putnik@example.com", adminJson);
    }

    [Fact]
    public async Task HealthEndpoints_ReportLiveAndReady()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task RouteGeometryEndpoint_ReturnsRoadPointsAndPublicCacheHeader()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/maps/route?startLat=45.815010&startLng=15.981919&endLat=43.508133&endLng=16.440193");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"points\"", json);
        Assert.Contains("45.81501", json);
        Assert.Contains("43.508133", json);
        Assert.Contains("max-age=604800", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task PendingAction_IsOwnedSingleUseAndAudited()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var pending = scope.ServiceProvider.GetRequiredService<IPendingActionService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var admin = CreatePrincipal(1, "Admin", "Driver", "Passenger");
        var passenger = CreatePrincipal(2, "Passenger");

        var action = pending.Create(
            admin,
            SideSeatActionTypes.CreateCity,
            "Novi grad",
            "Kreiranje grada Osijek",
            new CreateCityCommand("Osijek", "Hrvatska", "31000"));

        var foreignResult = await pending.ConfirmAsync(
            action.Token,
            passenger,
            "AI",
            CancellationToken.None);
        Assert.Equal(CommandErrorKind.Forbidden, foreignResult.ErrorKind);

        var result = await pending.ConfirmAsync(
            action.Token,
            admin,
            "AI",
            CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.True(await db.Gradovi.AnyAsync(city => city.Naziv == "Osijek"));

        var replay = await pending.ConfirmAsync(
            action.Token,
            admin,
            "AI",
            CancellationToken.None);
        Assert.Equal(CommandErrorKind.NotFound, replay.ErrorKind);
        Assert.Contains(
            await db.AuditLogs.ToListAsync(),
            log => log.Action == SideSeatActionTypes.CreateCity && log.Succeeded);
    }

    [Fact]
    public async Task AiPrepareTool_ReturnsConfirmationTokenWithoutWriting()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var admin = CreatePrincipal(1, "Admin", "Driver", "Passenger");

        var json = await tools.ExecuteAsync(
            "prepare_create_city",
            """{"naziv":"Dubrovnik","drzava":"Hrvatska","postanskiBroj":"20000"}""",
            admin,
            CancellationToken.None);

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.GetProperty("requiresConfirmation").GetBoolean());
        Assert.False(await db.Gradovi.AnyAsync(city => city.Naziv == "Dubrovnik"));
        var pendingAction = document.RootElement.GetProperty("pendingAction");
        Assert.True(pendingAction.TryGetProperty("token", out var token), json);
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
        Assert.True(pendingAction.TryGetProperty("form", out var form), json);
        Assert.Contains(token.GetString()!, form.GetProperty("reviewUrl").GetString());
    }

    [Fact]
    public async Task AiConfirmedCity_ReturnsBusinessRuleWhenGeocodingFails()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();
        var pending = scope.ServiceProvider.GetRequiredService<IPendingActionService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var admin = CreatePrincipal(1, "Admin", "Driver", "Passenger");

        var prepared = await tools.ExecuteAsync(
            "prepare_create_city",
            """{"naziv":"Bez Lokacije","drzava":"Hrvatska","postanskiBroj":"99999"}""",
            admin,
            CancellationToken.None);
        using var document = JsonDocument.Parse(prepared);
        var token = document.RootElement
            .GetProperty("pendingAction")
            .GetProperty("token")
            .GetString();

        var result = await pending.ConfirmAsync(token!, admin, "AI", CancellationToken.None);

        Assert.Equal(CommandErrorKind.BusinessRule, result.ErrorKind);
        Assert.False(await db.Gradovi.AnyAsync(city => city.Naziv == "Bez Lokacije"));
    }

    [Fact]
    public async Task McpCityTool_AcceptsManualCoordinatesAndConfirmsWrite()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var principal = CreatePrincipal(1, "Admin", "Driver", "Passenger");
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext { User = principal };
        var tools = new SideSeatMcpTools(
            scope.ServiceProvider.GetRequiredService<IAiToolService>(),
            scope.ServiceProvider.GetRequiredService<IPendingActionService>(),
            accessor);

        var prepared = await tools.PrepareCreateCity(
            "MCP Grad",
            "Hrvatska",
            "10001",
            45.900001m,
            16.100001m);
        using var preparedDocument = JsonDocument.Parse(prepared);
        var token = preparedDocument.RootElement
            .GetProperty("pendingAction")
            .GetProperty("token")
            .GetString();
        var confirmed = await tools.ConfirmAction(token!);
        using var confirmedDocument = JsonDocument.Parse(confirmed);

        Assert.True(confirmedDocument.RootElement.GetProperty("Succeeded").GetBoolean());
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var city = await db.Gradovi.SingleAsync(item => item.Naziv == "MCP Grad");
        Assert.Equal(45.900001m, city.Latitude);
        Assert.Equal(16.100001m, city.Longitude);
    }

    [Fact]
    public async Task AiCreateUserTool_MasksPasswordInPendingFormAndConfirmsWrite()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();
        var pending = scope.ServiceProvider.GetRequiredService<IPendingActionService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var admin = CreatePrincipal(1, "Admin", "Driver", "Passenger");

        var prepared = await tools.ExecuteAsync(
            "prepare_create_user",
            """
            {
              "ime": "AI",
              "prezime": "Korisnik",
              "email": "ai.korisnik@example.com",
              "adresa": "Test adresa 1",
              "brojMobitela": "0911234567",
              "tip": "Putnik",
              "jeAktivan": true,
              "password": "Secret123!"
            }
            """,
            admin,
            CancellationToken.None);
        using var preparedDocument = JsonDocument.Parse(prepared);
        var pendingAction = preparedDocument.RootElement.GetProperty("pendingAction");
        var token = pendingAction.GetProperty("token").GetString();
        var passwordField = pendingAction
            .GetProperty("form")
            .GetProperty("sections")[0]
            .GetProperty("fields")
            .EnumerateArray()
            .Single(field => field.GetProperty("name").GetString() == nameof(CreateUserCommand.Password));

        Assert.True(passwordField.GetProperty("isSensitive").GetBoolean());
        Assert.True(string.IsNullOrEmpty(passwordField.GetProperty("value").GetString()));

        var result = await pending.ConfirmAsync(token!, admin, "AI", CancellationToken.None);

        Assert.True(result.Succeeded, result.Message);
        var user = await db.Korisnici.SingleAsync(item => item.Email == "ai.korisnik@example.com");
        Assert.Equal(TipKorisnika.Putnik, user.Tip);
        Assert.NotEqual("Secret123!", user.LozinkaHash);
    }

    [Fact]
    public async Task McpUpdateVehicleTool_ConfirmsThroughPendingAction()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var principal = CreatePrincipal(1, "Admin", "Driver", "Passenger");
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext { User = principal };
        var tools = new SideSeatMcpTools(
            scope.ServiceProvider.GetRequiredService<IAiToolService>(),
            scope.ServiceProvider.GetRequiredService<IPendingActionService>(),
            accessor);

        var prepared = await tools.PrepareUpdateVehicle(
            1,
            "Skoda",
            "Superb",
            "ZG-AI-25",
            2022,
            5,
            "Crna",
            6.1m,
            null);
        using var preparedDocument = JsonDocument.Parse(prepared);
        var token = preparedDocument.RootElement
            .GetProperty("pendingAction")
            .GetProperty("token")
            .GetString();
        var confirmed = await tools.ConfirmAction(token!);
        using var confirmedDocument = JsonDocument.Parse(confirmed);

        Assert.True(confirmedDocument.RootElement.GetProperty("Succeeded").GetBoolean());
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var vehicle = await db.Vozila.SingleAsync(item => item.Id == 1);
        Assert.Equal("Superb", vehicle.Model);
        Assert.Equal("ZG-AI-25", vehicle.Registracija);
    }

    [Fact]
    public async Task McpEndpoint_RejectsMissingOrInvalidBearerKey()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/mcp")).StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrong-key");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/mcp")).StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_AcceptsAuthenticatedInitializeRequest()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-mcp-key");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var request = new StringContent(
            """
            {
              "jsonrpc": "2.0",
              "id": 1,
              "method": "initialize",
              "params": {
                "protocolVersion": "2025-06-18",
                "capabilities": {},
                "clientInfo": { "name": "SideSeat tests", "version": "1.0" }
              }
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/mcp", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("serverInfo", content);
        Assert.Contains("protocolVersion", content);
    }

    [Fact]
    public void ApiEndpoints_DeclareAllowedHttpMethods()
    {
        _ = _factory.CreateClient();
        var dataSources = _factory.Services.GetServices<EndpointDataSource>();
        var apiEndpoints = dataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
                endpoint.RoutePattern.RawText?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        Assert.NotEmpty(apiEndpoints);
        Assert.All(
            apiEndpoints,
            endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<HttpMethodMetadata>()));
    }

    private static ClaimsPrincipal CreatePrincipal(int korisnikId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(SideSeatClaimTypes.KorisnikId, korisnikId.ToString()),
            new(ClaimTypes.NameIdentifier, korisnikId.ToString()),
            new(ClaimTypes.Name, $"user-{korisnikId}@example.com")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
