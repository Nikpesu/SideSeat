using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideSeat.Security;

namespace SideSeat.IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Auth"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authType = Request.Headers["X-Test-Auth"].ToString();
        var isAdmin = authType == "admin";
        var isDriver = authType == "driver";
        var korisnikId = isAdmin || isDriver ? "1" : "2";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, korisnikId),
            new(SideSeatClaimTypes.KorisnikId, korisnikId),
            new(ClaimTypes.Name, isAdmin || isDriver ? "admin@example.com" : "putnik@example.com"),
            new(ClaimTypes.Role, "Passenger")
        };
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        if (isAdmin || isDriver)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Driver"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
