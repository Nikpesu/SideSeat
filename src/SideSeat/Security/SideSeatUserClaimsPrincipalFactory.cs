using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SideSeat.Models;

namespace SideSeat.Security;

public class SideSeatUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole<int>>
{
    public SideSeatUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.KorisnikId.HasValue)
        {
            identity.AddClaim(new Claim(SideSeatClaimTypes.KorisnikId, user.KorisnikId.Value.ToString()));
        }

        return identity;
    }
}
