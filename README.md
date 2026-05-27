# CQRS Discussion - Sample Project

Demonstrates that **CQRS** and **async command dispatch** are independent architectural decisions.

## How to Run

```bash
docker-compose up --build
```

Swagger UI: http://localhost:5000/swagger

RabbitMQ Management: http://localhost:15672 (guest/guest)

## Endpoint Groups

| Group | Endpoints | Pattern |
|-------|-----------|---------|
| Application Services | `POST/GET /services/workspaces` | Traditional service layer, same model for reads/writes |
| CQRS (MediatR + Pipeline) | `POST/GET /cqrs/workspaces` | Command/query separation + validation/logging pipeline |
| CQRS (Plain Services) | `POST/GET /cqrs-plain/workspaces` | Command/query separation, no framework |
| Async Dispatch (Naive) | `POST /async/workspaces` (202), `GET /async/workspaces` | No tracking — shows the problems |
| Async Dispatch (Proper) | `POST /async-proper/workspaces` (202), `GET /async-proper/operations/{id}` | With operation tracking |

## Concepts

### CQRS (Command Query Responsibility Segregation)
Separating read and write paths. That's it. No library required. See `/cqrs-plain/*` — just two service interfaces (`IWorkspaceCommandService`, `IWorkspaceQueryService`).

### MediatR (mediator + pipeline)
A library that gives you a standardized per-request pipeline: validation, logging, transactions — applied uniformly to every handler. See `/cqrs/*` — POST with empty name returns 400 automatically via `ValidationBehavior`, logging fires via `LoggingBehavior`. **This is MediatR's real value, not CQRS itself.**

### Application Services
Traditional approach: one service class with both `Create()` and `GetById()`. Same model, same code path. Perfectly fine for simple CRUD with no read/write asymmetry.

### Repository Pattern
Shared data access abstraction (`IWorkspaceRepository`) used by all approaches above. Orthogonal to everything else.

### Async Command Dispatch (Resilience)
Publishing commands to RabbitMQ so they survive infrastructure failures. Two versions shown:

**Naive** (`/async/*`): Returns 202 immediately, no operation tracking. Try posting with empty name — you get 202 but the workspace is never created. Client has no way to know.

**Proper** (`/async-proper/*`): Returns 202 with an `operationId`. Poll `GET /async-proper/operations/{id}` to see if it completed or failed with an error message. This is the **minimum** required for async dispatch to be production-ready.

**Key question:** Is all this overhead worth it for basic CRUD? Compare to `/services/*` which just returns 201 immediately. Async dispatch makes sense for expensive operations (processing, imports, provisioning) — not for inserting a row in a database.

## Key Takeaways

- **CQRS ≠ MediatR.** CQRS is a pattern (separate read/write). MediatR is a library (mediator + pipeline). You can have either without the other.
- **MediatR's real value** is the uniform per-use-case pipeline (validation, logging, transactions) — not command/query separation.
- **Async dispatch is orthogonal** to both CQRS and MediatR. It's an infrastructure/resilience choice.
- **Services are fine** when you don't need a uniform pipeline across many use cases.
