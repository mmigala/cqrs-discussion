using System.Text;
using System.Text.Json;
using CqrsDemo.Api.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CqrsDemo.Api.Async;

public class RabbitMqConsumer(IServiceProvider services, IConnection connection, ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private const string QueueName = "workspace-commands";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var cmd = JsonSerializer.Deserialize<CreateWorkspaceMessage>(body);
                if (cmd is not null)
                {
                    using var scope = services.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
                    repo.Create(cmd.Name);
                    logger.LogInformation("Processed async workspace creation: {Name}", cmd.Name);
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
    }
}

public record CreateWorkspaceMessage(string Name);
