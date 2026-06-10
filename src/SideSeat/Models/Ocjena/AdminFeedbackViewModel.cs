using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Ocjena;

public sealed class AdminFeedbackViewModel
{
    public int OcjenaId { get; set; }
    public string ReviewAuthor { get; set; } = string.Empty;
    public int BrojZvjezdica { get; set; }
    public string ReviewComment { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Administratorski feedback")]
    public string Feedback { get; set; } = string.Empty;
}
