using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Vozilo
{
    [Key]
    public int Id { get; set; }
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Registracija { get; set; } = string.Empty;
    public int GodinaProizvodnje { get; set; }
    public int BrojSjedala { get; set; }
    public string Boja { get; set; } = string.Empty;
    public decimal ProsjecnaPotrosnja { get; set; }

    [ForeignKey(nameof(Vlasnik))]
    public int? VlasnikId { get; set; }
    public virtual Korisnik? Vlasnik { get; set; }
}