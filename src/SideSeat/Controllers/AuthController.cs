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

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _db.Korisnici
            .FirstOrDefaultAsync(k => k.Email == model.Email.Trim());
        if (user is null || !_passwordHashingService.Verify(model.Password, user.LozinkaHash))
        {
            ModelState.AddModelError(string.Empty, "Neispravan email ili lozinka.");
            return View(model);
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
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var emailExists = await _db.Korisnici.AnyAsync(k => k.Email == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Korisnik s ovim emailom vec postoji.");
            return View(model);
        }

        var localPart = normalizedEmail.Split('@')[0];
        var user = new Korisnik
        {
            Email = normalizedEmail,
            LozinkaHash = _passwordHashingService.Hash(model.Password),
            Adresa = model.Address.Trim(),
            Ime = string.IsNullOrWhiteSpace(localPart) ? "Korisnik" : localPart,
            Prezime = string.Empty,
            BrojMobitela = string.Empty,
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
}
