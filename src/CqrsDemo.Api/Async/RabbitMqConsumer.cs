using System.Text;
using System.Text.Json;
using CqrsDemo.Api.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CqrsDemo.Api.Async;

public class RabbitMqConsumer(IServiceProvider services, IConnection connection, ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private const string NaiveQueue = "workspace-commands";
    private const string ProperQueue = "workspace-commands-proper";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(NaiveQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(ProperQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        // Naive consumer — no operation tracking, failures are silent
        var naiveConsumer = new AsyncEventingBasicConsumer(channel);
        naiveConsumer.ReceivedAsync += async (_, ea) =>
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
                    logger.LogInformation("[Naive] Processed: {Name}", cmd.Name);
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Naive] Failed silently — client already got 202");
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
        };

        // Proper consumer — tracks operation status, reports failures
        var properConsumer = new AsyncEventingBasicConsumer(channel);
        properConsumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var cmd = JsonSerializer.Deserialize<TrackedCreateWorkspaceMessage>(body);
                if (cmd is not null)
                {
                    using var scope = services.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
                    var opStore = scope.ServiceProvider.GetRequiredService<OperationStore>();

                    // Simulate validation that can only happen at processing time
                    if (string.IsNullOrWhiteSpace(cmd.Name))
                    {
                        opStore.Fail(cmd.OperationId, "Workspace name is required");
                        logger.LogWarning("[Proper] Operation {OpId} failed: empty name", cmd.OperationId);
                    }
                    else
                    {
                        var ws = repo.Create(cmd.Name);
                        opStore.Complete(cmd.OperationId, ws.Id);
                        logger.LogInformation("[Proper] Operation {OpId} completed: {Id}", cmd.OperationId, ws.Id);
                    }
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Proper] Failed to process tracked command");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(NaiveQueue, autoAck: false, naiveConsumer, stoppingToken);
        await channel.BasicConsumeAsync(ProperQueue, autoAck: false, properConsumer, stoppingToken);
    }
}

public record CreateWorkspaceMessage(string Name);
