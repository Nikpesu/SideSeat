using SideSeat.Models;
using SideSeat.Models.Ocjena;

namespace SideSeat.Models.Rides;

public class VoznjaDetailsViewModel
{
    public Voznja Voznja { get; set; } = null!;
    public List<VoznjaPassengerRow> Putnici { get; set; } = new();
    public List<VoznjaRatingRow> OcjeneVoznje { get; set; } = new();
    public double ProsjecnaOcjenaVoznje { get; set; }
    public int BrojOcjenaVoznje { get; set; }
    public List<VoznjaRatingRow> OcjeneVozaca { get; set; } = new();
    public double ProsjecnaOcjenaVozaca { get; set; }
    public int BrojOcjenaVozaca { get; set; }
}

public class VoznjaPassengerRow
{
    public int RezervacijaId { get; set; }
    public int PutnikId { get; set; }
    public string PutnikIme { get; set; } = string.Empty;
    public StatusRezervacije Status { get; set; }
    public int BrojMjesta { get; set; }
    public bool VozacJeOcijenio { get; set; }
    public bool PutnikJeOcijenio { get; set; }
}

public class VoznjaRatingRow
{
    public int OcjenaId { get; set; }
    public int RezervacijaId { get; set; }
    public string AutorIme { get; set; } = string.Empty;
    public string PrimateljIme { get; set; } = string.Empty;
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
    public DateTime? Uredeno { get; set; }
    public string? AdminFeedback { get; set; }
    public DateTime? AdminFeedbackAt { get; set; }
    public string? AdminFeedbackAuthor { get; set; }
    public List<OcjenaSlikaViewModel> Slike { get; set; } = new();
}
