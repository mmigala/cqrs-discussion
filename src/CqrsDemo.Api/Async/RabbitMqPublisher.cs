using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CqrsDemo.Api.Async;

public class RabbitMqPublisher(IConnection connection)
{
    private const string QueueName = "workspace-commands";

    public async Task PublishCreateCommand(string name)
    {
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(new CreateWorkspaceMessage(name));
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync("", QueueName, body);
    }
}
