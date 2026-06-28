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
    public decimal CashDue { get; set; }
    public decimal SaldoDue { get; set; }
    public bool AllConfirmedPassengersReady { get; set; }
}

public class VoznjaPassengerRow
{
    public int RezervacijaId { get; set; }
    public int PutnikId { get; set; }
    public string PutnikIme { get; set; } = string.Empty;
    public StatusRezervacije Status { get; set; }
    public int BrojMjesta { get; set; }
    public string? PutnikTelefon { get; set; }
    public NacinPlacanja NacinPlacanja { get; set; }
    public decimal CijenaUkupno { get; set; }
    public decimal Napojnica { get; set; }
    public DateTime? CheckInAtUtc { get; set; }
    public decimal? LastLatitude { get; set; }
    public decimal? LastLongitude { get; set; }
    public DateTime? LastLocationAtUtc { get; set; }
    public DateTime? CashCollectedAtUtc { get; set; }
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

public class CurrentRideViewModel
{
    public IReadOnlyList<CurrentRideItemViewModel> Rides { get; set; } = Array.Empty<CurrentRideItemViewModel>();
}

public class CurrentRideItemViewModel
{
    public Voznja Voznja { get; set; } = null!;
    public IReadOnlyList<VoznjaPassengerRow> Putnici { get; set; } = Array.Empty<VoznjaPassengerRow>();
    public IReadOnlyList<RideChatMessage> Messages { get; set; } = Array.Empty<RideChatMessage>();
    public bool AllReady { get; set; }
    public decimal CashDue { get; set; }

    // Perspektiva prijavljenog korisnika za ovu vožnju.
    public bool ViewerIsDriver { get; set; }
    public VoznjaPassengerRow? ViewerReservation { get; set; }
    public bool ViewerCanCheckIn { get; set; }
}

public class RideSettlementViewModel
{
    public Voznja Voznja { get; set; } = null!;
    public IReadOnlyList<VoznjaPassengerRow> Putnici { get; set; } = Array.Empty<VoznjaPassengerRow>();
    public decimal CashDue { get; set; }
    public decimal SaldoDue { get; set; }
    public bool HasCashDue => CashDue > 0;
}
