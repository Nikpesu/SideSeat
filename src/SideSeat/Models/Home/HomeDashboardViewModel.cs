using SideSeat.Models;

namespace SideSeat.Models.Home;

public class HomeDashboardViewModel
{
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public int BrojGradova { get; set; }
    public int BrojKorisnika { get; set; }
    public int BrojVoznji { get; set; }
    public int BrojRezervacija { get; set; }
    public int BrojOcjena { get; set; }
    public int BrojAktivnihVoznji { get; set; }
    public int BrojMojihVoznji { get; set; }
    public int BrojMojihRezervacija { get; set; }
    public List<HomeVoznjaRow> NadolazeceVoznje { get; set; } = new();
    public int? SearchFrom { get; set; }
    public int? SearchTo { get; set; }
    public string SearchFromText { get; set; } = string.Empty;
    public string SearchToText { get; set; } = string.Empty;
    public DateTime? SearchDate { get; set; }
    public List<HomeVoznjaSearchRow> SearchResults { get; set; } = new();
}

public class HomeVoznjaRow
{
    public DateTime Polazak { get; set; }
    public string PolazniGrad { get; set; } = string.Empty;
    public string OdredisniGrad { get; set; } = string.Empty;
    public StatusVoznje Status { get; set; }
}

public class HomeVoznjaSearchRow
{
    public int Id { get; set; }
    public DateTime Polazak { get; set; }
    public string PolazniGrad { get; set; } = string.Empty;
    public string OdredisniGrad { get; set; } = string.Empty;
    public int SlobodnaMjesta { get; set; }
    public decimal CijenaPoMjestu { get; set; }
    public string Vozac { get; set; } = string.Empty;
}
