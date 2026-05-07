using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Rezervacija
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Voznja))]
    public int VoznjaId { get; set; }
    public virtual Voznja Voznja { get; set; } = null!;

    [ForeignKey(nameof(Putnik))]
    public int PutnikId { get; set; }
    public virtual Korisnik Putnik { get; set; } = null!;
    public int BrojMjesta { get; set; }
    public decimal CijenaUkupno { get; set; }
    public DateTime VrijemeRezervacije { get; set; }
    public StatusRezervacije Status { get; set; }
    public string Napomena { get; set; } = string.Empty;
}