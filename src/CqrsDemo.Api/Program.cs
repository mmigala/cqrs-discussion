using CqrsDemo.Api.Async;
using CqrsDemo.Api.Cqrs;
using CqrsDemo.Api.Repository;
using CqrsDemo.Api.Services;
using MediatR;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateWorkspaceCommand>());

// RabbitMQ
var rabbitHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = rabbitHost,
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
    };
    // Retry connection to handle container startup ordering
    for (int i = 0; i < 10; i++)
    {
        try
        {
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        }
        catch
        {
            Thread.Sleep(2000);
        }
    }
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// --- Approach 2: Application Services (no CQRS) ---
app.MapPost("/services/workspaces", (CreateRequest req, IWorkspaceService svc) =>
{
    var ws = svc.Create(req.Name);
    return Results.Created($"/services/workspaces/{ws.Id}", ws);
}).WithTags("Application Services");

app.MapGet("/services/workspaces/{id:guid}", (Guid id, IWorkspaceService svc) =>
    svc.GetById(id) is { } ws ? Results.Ok(ws) : Results.NotFound())
    .WithTags("Application Services");

// --- Approach 1: CQRS via MediatR ---
app.MapPost("/cqrs/workspaces", async (CreateRequest req, IMediator mediator) =>
{
    var ws = await mediator.Send(new CreateWorkspaceCommand(req.Name));
    return Results.Created($"/cqrs/workspaces/{ws.Id}", ws);
}).WithTags("CQRS (MediatR)");

app.MapGet("/cqrs/workspaces/{id:guid}", async (Guid id, IMediator mediator) =>
    await mediator.Send(new GetWorkspaceQuery(id)) is { } ws ? Results.Ok(ws) : Results.NotFound())
    .WithTags("CQRS (MediatR)");

// --- Approach 4: Async Command Dispatch (RabbitMQ for resilience) ---
app.MapPost("/async/workspaces", async (CreateRequest req, RabbitMqPublisher publisher) =>
{
    await publisher.PublishCreateCommand(req.Name);
    return Results.Accepted(value: new { message = "Command queued for processing", name = req.Name });
}).WithTags("Async Dispatch (RabbitMQ)");

app.MapGet("/async/workspaces", (IWorkspaceRepository repo) =>
    Results.Ok(repo.GetAll()))
    .WithTags("Async Dispatch (RabbitMQ)");

app.Run();

public record CreateRequest(string Name);
