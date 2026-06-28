using System.Security.Claims;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SideSeat.Models.Commands;
using SideSeat.Security;

namespace SideSeat.Services;

public sealed class PendingActionService(
    IMemoryCache cache,
    ISideSeatCommandService commands) : IPendingActionService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PendingActionDescriptor Create<T>(
        ClaimsPrincipal principal,
        string actionType,
        string title,
        string summary,
        T payload)
    {
        var korisnikId = principal.GetKorisnikId()
            ?? throw new InvalidOperationException("Korisnik nije prijavljen.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        var envelope = new PendingActionEnvelope(
            actionType,
            korisnikId,
            title,
            summary,
            JsonSerializer.Serialize(payload, JsonOptions),
            expiresAt,
            BuildForm(token, actionType, title, summary, payload));
        cache.Set(CacheKey(token), envelope, expiresAt);
        return ToDescriptor(token, envelope);
    }

    public PendingActionDescriptor? GetDescriptor(string token, ClaimsPrincipal principal)
    {
        if (!cache.TryGetValue(CacheKey(token), out PendingActionEnvelope? action) ||
            action is null ||
            action.ExpiresAtUtc <= DateTime.UtcNow)
        {
            cache.Remove(CacheKey(token));
            return null;
        }

        if (principal.GetKorisnikId() != action.KorisnikId)
        {
            return null;
        }

        return ToDescriptor(token, action);
    }

    public async Task<CommandResult> ConfirmAsync(
        string token,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken)
    {
        var korisnikId = principal.GetKorisnikId();
        if (korisnikId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        if (!cache.TryGetValue(CacheKey(token), out PendingActionEnvelope? action) ||
            action is null ||
            action.ExpiresAtUtc <= DateTime.UtcNow)
        {
            cache.Remove(CacheKey(token));
            return CommandResult.Fail(CommandErrorKind.NotFound, "Akcija je istekla ili ne postoji.");
        }

        if (action.KorisnikId != korisnikId.Value)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Akcija pripada drugom korisniku.");
        }

        cache.Remove(CacheKey(token));
        return await commands.ExecutePendingAsync(action, principal, source, cancellationToken);
    }

    public bool Cancel(string token, ClaimsPrincipal principal)
    {
        if (!cache.TryGetValue(CacheKey(token), out PendingActionEnvelope? action) || action is null)
        {
            return false;
        }

        if (principal.GetKorisnikId() != action.KorisnikId)
        {
            return false;
        }

        cache.Remove(CacheKey(token));
        return true;
    }

    private static string CacheKey(string token) => $"sideseat:pending-action:{token}";

    private static PendingActionDescriptor ToDescriptor(string token, PendingActionEnvelope action) =>
        new(
            token,
            action.ActionType,
            action.Title,
            action.Summary,
            action.ExpiresAtUtc,
            action.Form);

    private static PendingFormDescriptor BuildForm<T>(
        string token,
        string actionType,
        string title,
        string summary,
        T payload)
    {
        var fields = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Select(property =>
            {
                var sensitive = IsSensitive(property.Name);
                return new PendingFormField(
                    property.Name,
                    Humanize(property.Name),
                    sensitive ? string.Empty : FormatValue(property.GetValue(payload)),
                    InputTypeFor(property.PropertyType),
                    sensitive);
            })
            .ToList();

        var warnings = new List<string>();
        if (actionType.Contains("delete", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Brisanje može ukloniti povezane podatke ili biti odbijeno ako zapis ima aktivne veze.");
        }

        if (summary.Length > 0)
        {
            warnings.Add(summary);
        }

        return new PendingFormDescriptor(
            actionType,
            title,
            actionType.Contains("delete", StringComparison.OrdinalIgnoreCase) ? "Potvrdi brisanje" : "Submit",
            $"/AiAction/Review/{token}",
            [new PendingFormSection("Podaci za provjeru", fields)],
            warnings);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var chars = new List<char> { value[0] };
        foreach (var character in value.Skip(1))
        {
            if (char.IsUpper(character))
            {
                chars.Add(' ');
            }

            chars.Add(character);
        }

        return new string(chars.ToArray());
    }

    private static string FormatValue(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm"),
            decimal number => number.ToString("0.##"),
            Enum enumValue => enumValue.ToString(),
            _ => value.ToString() ?? string.Empty
        };

    private static string InputTypeFor(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        if (actualType == typeof(DateTime))
        {
            return "datetime-local";
        }

        if (actualType == typeof(int) || actualType == typeof(decimal) || actualType == typeof(double))
        {
            return "number";
        }

        return "text";
    }

    private static bool IsSensitive(string name) =>
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("lozinka", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("oib", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("api", StringComparison.OrdinalIgnoreCase);
}
