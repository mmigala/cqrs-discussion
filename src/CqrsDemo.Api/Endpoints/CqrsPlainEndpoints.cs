using CqrsDemo.Api.CqrsPlain;

namespace CqrsDemo.Api.Endpoints;

public static class CqrsPlainEndpoints
{
    public static void MapCqrsPlainEndpoints(this WebApplication app)
    {
        app.MapPost("/cqrs-plain/workspaces", (CreateRequest req, IWorkspaceCommandService cmdSvc) =>
        {
            var ws = cmdSvc.Create(req.Name);
            return Results.Created($"/cqrs-plain/workspaces/{ws.Id}", ws);
        }).WithTags("CQRS (Plain Services)");

        app.MapGet("/cqrs-plain/workspaces/{id:guid}", (Guid id, IWorkspaceQueryService querySvc) =>
            querySvc.GetById(id) is { } ws ? Results.Ok(ws) : Results.NotFound())
            .WithTags("CQRS (Plain Services)");
    }
}
