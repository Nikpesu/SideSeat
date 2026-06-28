using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Obavijest
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Korisnik))]
    public int KorisnikId { get; set; }
    public virtual Korisnik Korisnik { get; set; } = null!;

    [MaxLength(120)]
    public string Naslov { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Poruka { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Tip { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Link { get; set; }

    public DateTime Kreirano { get; set; }
    public bool Procitano { get; set; }
}
