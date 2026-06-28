using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models;

public sealed class AuditLog
{
    public long Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int? KorisnikId { get; set; }

    [StringLength(120)]
    public string Actor { get; set; } = string.Empty;

    [StringLength(40)]
    public string Source { get; set; } = string.Empty;

    [StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [StringLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityId { get; set; }

    public bool Succeeded { get; set; }

    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    [StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;
}
