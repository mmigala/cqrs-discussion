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
| CQRS (MediatR) | `POST/GET /cqrs/workspaces` | Command/query separation, in-process mediator |
| Async Dispatch | `POST /async/workspaces` (202), `GET /async/workspaces` | Queue-based writes for resilience |

## Concepts

### CQRS (Command Query Responsibility Segregation)
Separating read and write models/handlers. In this demo, `CreateWorkspaceCommand` and `GetWorkspaceQuery` are separate objects handled by separate classes. All in-process via MediatR — no queues needed.

### Application Services
Traditional approach: one service class with both `Create()` and `GetById()`. Same model, same code path. Perfectly fine for simple CRUD with no read/write asymmetry.

### Repository Pattern
Shared data access abstraction (`IWorkspaceRepository`) used by both approaches above. Orthogonal to CQRS — it sits below either pattern.

### Async Command Dispatch (Resilience)
Publishing commands to RabbitMQ so they survive infrastructure failures. The API returns HTTP 202 immediately; a background worker processes the message later. This is a **deployment decision**, not an architectural pattern. Trade-off: no synchronous validation response.

## Key Takeaway

You can mix and match:
- CQRS without queues ✓ (this demo: `/cqrs/*`)
- Queues without CQRS ✓ (this demo: `/async/*` uses the same model for reads/writes)
- Neither ✓ (this demo: `/services/*`)
