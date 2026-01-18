using MassTransit;
using NotificationsAPI.Shared.Events;

namespace NotificationsAPI.Consumers;

public class PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        if (message.Status == PaymentStatus.Approved)
        {
            logger.LogInformation("[NotificationsAPI] Purchase approved for UserId={UserId}, GameId={GameId}, Price={Price}", message.UserId, message.GameId, message.Price);
            await Task.Delay(2000);
            logger.LogInformation("[NotificationsAPI] Purchase confirmation email sent to UserId={UserId}", message.UserId);
        }
        else
        {
            logger.LogInformation("[NotificationsAPI] Purchase rejected for UserId={UserId}, GameId={GameId}", message.UserId, message.GameId);
        }
    }
}
