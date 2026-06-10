using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Ai;

public sealed class AiChatMessage
{
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
