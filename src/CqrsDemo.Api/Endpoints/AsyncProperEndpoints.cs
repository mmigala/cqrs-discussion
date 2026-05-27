using CqrsDemo.Api.Async;

namespace CqrsDemo.Api.Endpoints;

public static class AsyncProperEndpoints
{
    public static void MapAsyncProperEndpoints(this WebApplication app)
    {
        // PROPER: Operation tracking — client can poll for status
        // This is the MINIMUM required for async dispatch to be production-ready
        app.MapPost("/async-proper/workspaces", async (CreateRequest req, RabbitMqPublisher publisher, OperationStore opStore) =>
        {
            var operation = opStore.Create();
            await publisher.PublishTrackedCommand(operation.Id, req.Name);
            return Results.Accepted($"/async-proper/operations/{operation.Id}", new { operationId = operation.Id });
        }).WithTags("Async Dispatch (Proper)");

        // Poll this to know if the command succeeded or failed
        app.MapGet("/async-proper/operations/{id:guid}", (Guid id, OperationStore opStore) =>
            opStore.GetById(id) is { } op ? Results.Ok(op) : Results.NotFound())
            .WithTags("Async Dispatch (Proper)");
    }
}
