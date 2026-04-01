namespace SideSeat.Models;

public class Rezervacija
{
    public int Id { get; set; }
    public int VoznjaId { get; set; }
    public Voznja Voznja { get; set; } = null!;
    public int PutnikId { get; set; }
    public Korisnik Putnik { get; set; } = null!;
    public int BrojMjesta { get; set; }
    public decimal CijenaUkupno { get; set; }
    public DateTime VrijemeRezervacije { get; set; }
    public StatusRezervacije Status { get; set; }
    public string Napomena { get; set; } = string.Empty;
}