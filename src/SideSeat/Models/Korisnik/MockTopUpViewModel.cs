using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Balance;

public class MockTopUpViewModel
{
    [Range(0.01, 1000000, ErrorMessage = "Iznos mora biti veci od 0.")]
    [Display(Name = "Iznos uplate")]
    public decimal Iznos { get; set; }

    [Required]
    [Display(Name = "Način plaćanja")]
    public string NacinPlacanja { get; set; } = "Kartica";

    [Display(Name = "Ime na kartici")]
    public string? CardholderName { get; set; }

    [Display(Name = "Broj kartice")]
    public string? CardNumber { get; set; }

    [Display(Name = "Vrijedi do")]
    public string? CardExpiry { get; set; }

    [Display(Name = "CVV")]
    public string? CardCvv { get; set; }

    [Display(Name = "Ime računa")]
    public string? ExternalAccountName { get; set; }

    public bool ExternalPaymentConfirmed { get; set; }

    [Display(Name = "Spremi karticu za buduće mock uplate")]
    public bool SaveCard { get; set; }

    [Display(Name = "Ulica")]
    public string? BillingStreet { get; set; }

    [Display(Name = "Kućni broj")]
    public string? BillingHouseNumber { get; set; }

    [Display(Name = "Poštanski broj")]
    public string? BillingPostalCode { get; set; }

    [Display(Name = "Država")]
    public string? BillingCountry { get; set; }

    [Display(Name = "Spremi adresu plaćanja")]
    public bool SaveBillingAddress { get; set; }

    public string? SavedCardDisplay { get; set; }

    public string? ReturnUrl { get; set; }
}
