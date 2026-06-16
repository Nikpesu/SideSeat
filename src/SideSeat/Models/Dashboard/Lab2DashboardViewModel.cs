namespace SideSeat.Models.Dashboard;

/// <summary>
/// Sadrzi brojace po entitetima za prikaz na Home dashboard stranici.
/// </summary>
public class Lab2DashboardViewModel
{
    public bool IsAdmin { get; set; }
    public int BrojGradova { get; set; }
    public int BrojKorisnika { get; set; }
    public int BrojVozila { get; set; }
    public int BrojVoznji { get; set; }
    public int BrojRezervacija { get; set; }
    public int BrojPlacanja { get; set; }
    public int BrojOcjena { get; set; }
    public int BrojMojihVoznji { get; set; }
    public int BrojMojihRezervacija { get; set; }
    public int BrojAktivnihVoznji { get; set; }
}
