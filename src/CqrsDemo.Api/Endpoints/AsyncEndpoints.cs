using CqrsDemo.Api.Async;
using CqrsDemo.Api.Repository;

namespace CqrsDemo.Api.Endpoints;

public static class AsyncEndpoints
{
    public static void MapAsyncEndpoints(this WebApplication app)
    {
        // NAIVE: No validation feedback, no operation tracking, eventual consistency
        // Try: POST with empty name → still returns 202 (failure is silent)
        // Try: POST then immediately GET → workspace may not exist yet
        app.MapPost("/async/workspaces", async (CreateRequest req, RabbitMqPublisher publisher) =>
        {
            // No validation here! Even invalid commands get accepted.
            await publisher.PublishCreateCommand(req.Name);
            return Results.Accepted(value: new { message = "Command queued — but did it actually work? You won't know.", name = req.Name });
        }).WithTags("Async Dispatch (Naive)");

        app.MapGet("/async/workspaces", (IWorkspaceRepository repo) =>
            Results.Ok(repo.GetAll()))
            .WithTags("Async Dispatch (Naive)");
    }
}
