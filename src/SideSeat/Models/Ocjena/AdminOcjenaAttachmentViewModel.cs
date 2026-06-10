namespace SideSeat.Models.Ocjena;

public class AdminOcjenaAttachmentViewModel
{
    public int ImageId { get; set; }
    public int OcjenaId { get; set; }
    public int RezervacijaId { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}
