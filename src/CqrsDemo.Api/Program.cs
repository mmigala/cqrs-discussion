using CqrsDemo.Api.Async;
using CqrsDemo.Api.Cqrs;
using CqrsDemo.Api.Cqrs.Behaviors;
using CqrsDemo.Api.CqrsPlain;
using CqrsDemo.Api.Endpoints;
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

app.MapServiceEndpoints();
app.MapCqrsEndpoints();
app.MapCqrsPlainEndpoints();
app.MapAsyncEndpoints();

app.Run();

public record CreateRequest(string Name);
