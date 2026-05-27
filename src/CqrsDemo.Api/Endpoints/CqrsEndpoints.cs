using CqrsDemo.Api.Cqrs;
using MediatR;

namespace CqrsDemo.Api.Endpoints;

public static class CqrsEndpoints
{
    public static void MapCqrsEndpoints(this WebApplication app)
    {
        // Pipeline: ValidationBehavior → LoggingBehavior → Handler
        app.MapPost("/cqrs/workspaces", async (CreateRequest req, IMediator mediator) =>
        {
            var ws = await mediator.Send(new CreateWorkspaceCommand(req.Name));
            return Results.Created($"/cqrs/workspaces/{ws.Id}", ws);
        }).WithTags("CQRS (MediatR + Pipeline)");

        app.MapGet("/cqrs/workspaces/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetWorkspaceQuery(id)) is { } ws ? Results.Ok(ws) : Results.NotFound())
            .WithTags("CQRS (MediatR + Pipeline)");
    }
}
