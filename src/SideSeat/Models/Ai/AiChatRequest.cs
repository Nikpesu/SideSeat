using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Ai;

public sealed class AiChatRequest
{
    [Required]
    [MinLength(1)]
    public List<AiChatMessage> Messages { get; set; } = [];

    [StringLength(200)]
    public string? PageTitle { get; set; }

    [StringLength(500)]
    public string? PagePath { get; set; }
}
