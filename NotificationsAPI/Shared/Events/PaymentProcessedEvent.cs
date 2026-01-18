namespace NotificationsAPI.Shared.Events;

public enum PaymentStatus { Approved, Rejected }

public record PaymentProcessedEvent(Guid UserId, Guid GameId, decimal Price, PaymentStatus Status);
