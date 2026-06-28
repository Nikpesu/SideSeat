using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SideSeat.Security;

namespace SideSeat.Middleware;

public sealed class McpApiKeyMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<McpApiKeyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["Mcp:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "MCP nije konfiguriran.",
                status = StatusCodes.Status503ServiceUnavailable
            });
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        var providedKey = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : string.Empty;
        if (!FixedTimeEquals(configuredKey, providedKey))
        {
            logger.LogWarning("Rejected MCP request with an invalid API key.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return;
        }

        var userId = configuration.GetValue<int?>("Mcp:KorisnikId") ?? 1;
        var roleList = configuration.GetSection("Mcp:Roles").Get<string[]>()
            ?? ["Admin", "Driver", "Passenger"];
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(SideSeatClaimTypes.KorisnikId, userId.ToString()),
            new(ClaimTypes.Name, configuration["Mcp:Actor"] ?? "sideseat-mcp")
        };
        claims.AddRange(roleList.Select(role => new Claim(ClaimTypes.Role, role)));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "McpApiKey"));
        await next(context);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
