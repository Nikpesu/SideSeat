using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class RideChatMessage
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Voznja))]
    public int VoznjaId { get; set; }
    public virtual Voznja Voznja { get; set; } = null!;

    [ForeignKey(nameof(Sender))]
    public int SenderId { get; set; }
    public virtual Korisnik Sender { get; set; } = null!;

    [ForeignKey(nameof(Recipient))]
    public int? RecipientId { get; set; }
    public virtual Korisnik? Recipient { get; set; }

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
