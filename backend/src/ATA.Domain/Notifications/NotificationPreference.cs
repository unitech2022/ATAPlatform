namespace ATA.Domain.Notifications;

public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public bool Trips { get; set; } = true;
    public bool Wallet { get; set; } = true;
    public bool Safety { get; set; } = true;
    public bool Offers { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
