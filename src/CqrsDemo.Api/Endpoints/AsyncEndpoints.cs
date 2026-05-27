using CqrsDemo.Api.Async;
using CqrsDemo.Api.Repository;

namespace CqrsDemo.Api.Endpoints;

public static class AsyncEndpoints
{
    public static void MapAsyncEndpoints(this WebApplication app)
    {
        app.MapPost("/async/workspaces", async (CreateRequest req, RabbitMqPublisher publisher) =>
        {
            await publisher.PublishCreateCommand(req.Name);
            return Results.Accepted(value: new { message = "Command queued for processing", name = req.Name });
        }).WithTags("Async Dispatch (RabbitMQ)");

        app.MapGet("/async/workspaces", (IWorkspaceRepository repo) =>
            Results.Ok(repo.GetAll()))
            .WithTags("Async Dispatch (RabbitMQ)");
    }
}
