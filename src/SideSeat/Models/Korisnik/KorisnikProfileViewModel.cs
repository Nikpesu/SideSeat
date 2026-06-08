using SideSeat.Models;
using SideSeat.Models.Ocjena;

namespace SideSeat.Models.ViewModels;

public class KorisnikProfileViewModel
{
    public Korisnik User { get; set; } = null!;
    public bool CanViewFullDetails { get; set; }
    public double ProsjecnaOcjena { get; set; }
    public int BrojPrimljenihOcjena { get; set; }
    public List<KorisnikOcjenaRow> PrimljeneOcjene { get; set; } = new();
    public List<KorisnikOcjenaRow> DaneOcjene { get; set; } = new();
}

public class KorisnikOcjenaRow
{
    public int RezervacijaId { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string Primatelj { get; set; } = string.Empty;
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
    public List<OcjenaSlikaViewModel> Slike { get; set; } = new();
}
