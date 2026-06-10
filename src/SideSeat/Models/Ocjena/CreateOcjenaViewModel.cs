using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SideSeat.Models.Ocjena;

public class CreateOcjenaViewModel
{
    public int RezervacijaId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public bool IsAdditional { get; set; }

    [Range(1, 5, ErrorMessage = "Ocjena mora biti izmedu 1 i 5.")]
    public int BrojZvjezdica { get; set; } = 5;

    [Required(ErrorMessage = "Komentar je obavezan.")]
    [StringLength(500, ErrorMessage = "Komentar moze imati najvise 500 znakova.")]
    public string Komentar { get; set; } = string.Empty;

    public List<IFormFile> Slike { get; set; } = new();
}
