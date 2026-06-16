using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class SaldoTransakcija
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Korisnik))]
    public int KorisnikId { get; set; }
    public virtual Korisnik Korisnik { get; set; } = null!;

    public decimal Iznos { get; set; }
    public string Tip { get; set; } = string.Empty; // uplata | isplata
    public string Komentar { get; set; } = string.Empty;
    public decimal SaldoPrije { get; set; }
    public decimal SaldoPoslije { get; set; }
    public DateTime Vrijeme { get; set; }
}
