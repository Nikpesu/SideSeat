using System.Security.Claims;

namespace SideSeat.Security;

public static class ClaimsPrincipalExtensions
{
    public static int? GetKorisnikId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
