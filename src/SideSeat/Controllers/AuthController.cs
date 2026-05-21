using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Auth;
using SideSeat.Services;

namespace SideSeat.Controllers;

public class AuthController : Controller
{
    private readonly SideSeatDbContext _db;
    private readonly IPasswordHashingService _passwordHashingService;

    public AuthController(SideSeatDbContext db, IPasswordHashingService passwordHashingService)
    {
        _db = db;
        _passwordHashingService = passwordHashingService;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Provjeri unesene podatke.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(model.ReturnUrl) });
        }

        var user = await _db.Korisnici
            .FirstOrDefaultAsync(k => k.Email == model.Email.Trim());
        if (user is null || !_passwordHashingService.Verify(model.Password, user.LozinkaHash))
        {
            ModelState.AddModelError(string.Empty, "Neispravan email ili lozinka.");
            TempData["AuthError"] = "Neispravan email ili lozinka.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(model.ReturnUrl) });
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Provjeri unesene podatke.";
            return RedirectToAction("Index", "Home", new { auth = "register" });
        }

        var normalizedEmail = model.Email.Trim();
        var emailExists = await _db.Korisnici.AnyAsync(k => k.Email == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Korisnik s ovim emailom vec postoji.");
            TempData["AuthError"] = "Korisnik s ovim emailom vec postoji.";
            return RedirectToAction("Index", "Home", new { auth = "register" });
        }

        var localPart = normalizedEmail.Split('@')[0];
        var user = new Korisnik
        {
            Email = normalizedEmail,
            LozinkaHash = _passwordHashingService.Hash(model.Password),
            Adresa = model.Address.Trim(),
            Ime = string.IsNullOrWhiteSpace(localPart) ? "Korisnik" : localPart,
            Prezime = string.Empty,
            BrojMobitela = model.PhoneNumber.Trim(),
            DatumRegistracije = DateTime.UtcNow,
            Tip = TipKorisnika.Putnik,
            JeAktivan = true,
            KycPodnesen = false
        };

        _db.Korisnici.Add(user);
        await _db.SaveChangesAsync();
        await SignInAsync(user, rememberMe: false);

        return RedirectToAction("Settings", "Korisnik");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task SignInAsync(Korisnik user, bool rememberMe)
    {
        var role = user.Tip == TipKorisnika.Admin ? "Admin" : "User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
    }

    private static string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        return returnUrl.Contains("auth=", StringComparison.OrdinalIgnoreCase) ? null : returnUrl;
    }
}
