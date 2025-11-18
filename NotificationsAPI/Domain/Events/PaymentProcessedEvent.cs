namespace NotificationsAPI.Domain.Events;

public enum PaymentStatus { Approved, Rejected }

public record PaymentProcessedEvent(Guid UserId, Guid GameId, decimal Price, PaymentStatus Status);
