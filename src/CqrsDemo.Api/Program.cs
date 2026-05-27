using CqrsDemo.Api.Async;
using CqrsDemo.Api.Cqrs;
using CqrsDemo.Api.Cqrs.Behaviors;
using CqrsDemo.Api.CqrsPlain;
using CqrsDemo.Api.Repository;
using CqrsDemo.Api.Services;
using FluentValidation;
using MediatR;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

// MediatR + pipeline behaviors (Phase 6: the real value of MediatR)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateWorkspaceCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorkspaceCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// CQRS without MediatR (Phase 7: proves CQRS ≠ MediatR)
builder.Services.AddScoped<IWorkspaceCommandService, WorkspaceCommandService>();
builder.Services.AddScoped<IWorkspaceQueryService, WorkspaceQueryService>();

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

// Map FluentValidation exceptions to 400 responses
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new
        {
            errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
        });
    }
});

// --- Approach 1: Application Services (no CQRS) ---
app.MapPost("/services/workspaces", (CreateRequest req, IWorkspaceService svc) =>
{
    var ws = svc.Create(req.Name);
    return Results.Created($"/services/workspaces/{ws.Id}", ws);
}).WithTags("Application Services");

app.MapGet("/services/workspaces/{id:guid}", (Guid id, IWorkspaceService svc) =>
    svc.GetById(id) is { } ws ? Results.Ok(ws) : Results.NotFound())
    .WithTags("Application Services");

// --- Approach 2: CQRS via MediatR (in-process, no queue) ---
// Pipeline: ValidationBehavior → LoggingBehavior → Handler
app.MapPost("/cqrs/workspaces", async (CreateRequest req, IMediator mediator) =>
{
    var ws = await mediator.Send(new CreateWorkspaceCommand(req.Name));
    return Results.Created($"/cqrs/workspaces/{ws.Id}", ws);
}).WithTags("CQRS (MediatR + Pipeline)");

app.MapGet("/cqrs/workspaces/{id:guid}", async (Guid id, IMediator mediator) =>
    await mediator.Send(new GetWorkspaceQuery(id)) is { } ws ? Results.Ok(ws) : Results.NotFound())
    .WithTags("CQRS (MediatR + Pipeline)");

// --- Approach 3: CQRS without MediatR (plain services, same pattern) ---
app.MapPost("/cqrs-plain/workspaces", (CreateRequest req, IWorkspaceCommandService cmdSvc) =>
{
    var ws = cmdSvc.Create(req.Name);
    return Results.Created($"/cqrs-plain/workspaces/{ws.Id}", ws);
}).WithTags("CQRS (Plain Services)");

app.MapGet("/cqrs-plain/workspaces/{id:guid}", (Guid id, IWorkspaceQueryService querySvc) =>
    querySvc.GetById(id) is { } ws ? Results.Ok(ws) : Results.NotFound())
    .WithTags("CQRS (Plain Services)");

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
