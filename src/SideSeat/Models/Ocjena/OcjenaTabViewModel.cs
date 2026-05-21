namespace SideSeat.Models.Ocjena;

public class OcjenaTabViewModel
{
    public List<OcjenaPendingItemViewModel> Pending { get; set; } = new();
    public List<OcjenaHistoryItemViewModel> Given { get; set; } = new();
    public List<OcjenaHistoryItemViewModel> Received { get; set; } = new();
    public double GivenAverage { get; set; }
    public double ReceivedAverage { get; set; }
}

public class OcjenaPendingItemViewModel
{
    public int RezervacijaId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string RouteLabel { get; set; } = string.Empty;
    public DateTime RideDate { get; set; }
}

public class OcjenaHistoryItemViewModel
{
    public int OcjenaId { get; set; }
    public int RezervacijaId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
    public string RouteLabel { get; set; } = string.Empty;
}
