using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Auth;

namespace SideSeat.Controllers;

public class AuthController : Controller
{
    private readonly SideSeatDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        SideSeatDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(returnUrl) });
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

        var normalizedEmail = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            TempData["AuthError"] = "Neispravan email ili lozinka.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(model.ReturnUrl) });
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = "Neispravan email ili lozinka.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(model.ReturnUrl) });
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return RedirectToAction("Index", "Home", new { auth = "register" });
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
        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            TempData["AuthError"] = "Korisnik s ovim emailom vec postoji.";
            return RedirectToAction("Index", "Home", new { auth = "register" });
        }

        var korisnik = CreateDomainUser(
            normalizedEmail,
            model.Address,
            model.PhoneNumber,
            TipKorisnika.Putnik);

        _db.Korisnici.Add(korisnik);
        await _db.SaveChangesAsync();

        var appUser = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            PhoneNumber = model.PhoneNumber.Trim(),
            OIB = model.OIB.Trim(),
            JMBG = model.JMBG.Trim(),
            KorisnikId = korisnik.Id
        };

        var result = await _userManager.CreateAsync(appUser, model.Password);
        if (!result.Succeeded)
        {
            _db.Korisnici.Remove(korisnik);
            await _db.SaveChangesAsync();
            TempData["AuthError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("Index", "Home", new { auth = "register" });
        }

        await _userManager.AddToRoleAsync(appUser, IdentityDataSeeder.PassengerRole);
        await _signInManager.SignInAsync(appUser, isPersistent: false);

        return RedirectToAction("Settings", "Korisnik");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!string.Equals(provider, "Google", StringComparison.Ordinal) || !IsGoogleConfigured())
        {
            TempData["AuthError"] = "Google prijava nije konfigurirana.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl = GetSafeReturnUrl(returnUrl) });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            TempData["AuthError"] = $"Vanjska prijava nije uspjela: {remoteError}";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["AuthError"] = "Nije moguce dohvatiti podatke vanjske prijave.";
            return RedirectToAction("Index", "Home", new { auth = "login", returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (signInResult.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var existingUser = string.IsNullOrWhiteSpace(email) ? null : await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
        }

        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel
        {
            Email = email,
            ReturnUrl = GetSafeReturnUrl(returnUrl)
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["AuthError"] = "Vanjska prijava je istekla. Pokusajte ponovno.";
            return RedirectToAction("Index", "Home", new { auth = "login" });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Korisnik s ovim emailom vec postoji.");
            return View(model);
        }

        var korisnik = CreateDomainUser(
            normalizedEmail,
            model.Address,
            model.PhoneNumber,
            TipKorisnika.Putnik);

        _db.Korisnici.Add(korisnik);
        await _db.SaveChangesAsync();

        var appUser = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            PhoneNumber = model.PhoneNumber.Trim(),
            OIB = model.OIB.Trim(),
            JMBG = model.JMBG.Trim(),
            KorisnikId = korisnik.Id
        };

        var createResult = await _userManager.CreateAsync(appUser);
        if (!createResult.Succeeded)
        {
            _db.Korisnici.Remove(korisnik);
            await _db.SaveChangesAsync();
            AddIdentityErrors(createResult);
            return View(model);
        }

        var loginResult = await _userManager.AddLoginAsync(appUser, info);
        if (!loginResult.Succeeded)
        {
            _db.Korisnici.Remove(korisnik);
            await _db.SaveChangesAsync();
            AddIdentityErrors(loginResult);
            return View(model);
        }

        await _userManager.AddToRoleAsync(appUser, IdentityDataSeeder.PassengerRole);
        await _signInManager.SignInAsync(appUser, isPersistent: false);

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private Korisnik CreateDomainUser(string email, string address, string phoneNumber, TipKorisnika tip)
    {
        var localPart = email.Split('@')[0];
        return new Korisnik
        {
            Email = email,
            LozinkaHash = string.Empty,
            Adresa = address.Trim(),
            Ime = string.IsNullOrWhiteSpace(localPart) ? "Korisnik" : localPart,
            Prezime = string.Empty,
            BrojMobitela = phoneNumber.Trim(),
            DatumRegistracije = DateTime.UtcNow,
            Tip = tip,
            JeAktivan = true,
            KycPodnesen = false
        };
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private bool IsGoogleConfigured() =>
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
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
