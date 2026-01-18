namespace Shared.Events;

public class UserCreatedEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
}
