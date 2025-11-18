namespace NotificationsAPI.Domain.Events;

public record UserCreatedEvent(Guid UserId, string Email, string Name);
