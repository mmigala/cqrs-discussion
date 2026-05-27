using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CqrsDemo.Api.Async;

public class RabbitMqPublisher(IConnection connection)
{
    private const string NaiveQueue = "workspace-commands";
    private const string ProperQueue = "workspace-commands-proper";

    public async Task PublishCreateCommand(string name)
    {
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(NaiveQueue, durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(new CreateWorkspaceMessage(name));
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync("", NaiveQueue, body);
    }

    public async Task PublishTrackedCommand(Guid operationId, string name)
    {
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(ProperQueue, durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(new TrackedCreateWorkspaceMessage(operationId, name));
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync("", ProperQueue, body);
    }
}

public record TrackedCreateWorkspaceMessage(Guid OperationId, string Name);
