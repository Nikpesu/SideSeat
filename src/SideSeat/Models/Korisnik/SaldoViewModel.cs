using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Balance;

public class SaldoViewModel
{
    public decimal TrenutniSaldo { get; set; }
    public List<SaldoTransakcijaRowViewModel> Transakcije { get; set; } = new();

    [Range(0.01, 1000000, ErrorMessage = "Iznos mora biti veci od 0.")]
    [Display(Name = "Iznos")]
    public decimal Iznos { get; set; }

    [Required]
    [Display(Name = "Akcija")]
    public string Akcija { get; set; } = "uplata";
}

public class SaldoTransakcijaRowViewModel
{
    public DateTime Vrijeme { get; set; }
    public string Tip { get; set; } = string.Empty;
    public decimal Iznos { get; set; }
    public decimal SaldoPrije { get; set; }
    public decimal SaldoPoslije { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public int? VoznjaId { get; set; }
}
