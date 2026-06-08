namespace SideSeat.Models.Notifications;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<NotificationBellItemViewModel> Items { get; set; } = Array.Empty<NotificationBellItemViewModel>();
}

public class NotificationBellItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
