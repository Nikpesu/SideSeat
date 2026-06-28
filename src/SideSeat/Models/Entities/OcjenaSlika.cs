using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class OcjenaSlika
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(OcjenaVoznje))]
    public int OcjenaVoznjeId { get; set; }
    public virtual OcjenaVoznje OcjenaVoznje { get; set; } = null!;

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
