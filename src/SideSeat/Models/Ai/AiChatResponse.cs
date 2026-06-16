namespace SideSeat.Models.Ai;

public sealed record AiChatResponse(
    string Message,
    string Model,
    SideSeat.Models.Commands.PendingActionDescriptor? PendingAction = null);
