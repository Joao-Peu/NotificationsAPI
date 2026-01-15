using MassTransit;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Workers;

public class UserCreatedConsumer(ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreatedEvent>
{
    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("[NotificationsAPI] Welcome email sent to {Email} (UserId={UserId})", message.Email, message.UserId);
        return Task.CompletedTask;
    }
}
