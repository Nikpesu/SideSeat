using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class VoznjaAttachment
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Voznja))]
    public int VoznjaId { get; set; }
    public virtual Voznja Voznja { get; set; } = null!;

    [Required]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}
