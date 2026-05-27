using CqrsDemo.Api.Services;

namespace CqrsDemo.Api.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        app.MapPost("/services/workspaces", (CreateRequest req, IWorkspaceService svc) =>
        {
            var ws = svc.Create(req.Name);
            return Results.Created($"/services/workspaces/{ws.Id}", ws);
        }).WithTags("Application Services");

        app.MapGet("/services/workspaces/{id:guid}", (Guid id, IWorkspaceService svc) =>
            svc.GetById(id) is { } ws ? Results.Ok(ws) : Results.NotFound())
            .WithTags("Application Services");
    }
}
