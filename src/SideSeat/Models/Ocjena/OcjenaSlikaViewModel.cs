namespace SideSeat.Models.Ocjena;

public class OcjenaSlikaViewModel
{
    public int Id { get; set; }
    public int OcjenaVoznjeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool CanDelete { get; set; }
}
