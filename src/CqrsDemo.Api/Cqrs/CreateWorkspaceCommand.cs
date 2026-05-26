using CqrsDemo.Api.Domain;
using CqrsDemo.Api.Repository;
using MediatR;

namespace CqrsDemo.Api.Cqrs;

public record CreateWorkspaceCommand(string Name) : IRequest<Workspace>;

public class CreateWorkspaceHandler(IWorkspaceRepository repository) : IRequestHandler<CreateWorkspaceCommand, Workspace>
{
    public Task<Workspace> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        => Task.FromResult(repository.Create(request.Name));
}
