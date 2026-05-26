using CqrsDemo.Api.Domain;
using CqrsDemo.Api.Repository;
using MediatR;

namespace CqrsDemo.Api.Cqrs;

public record GetWorkspaceQuery(Guid Id) : IRequest<Workspace?>;

public class GetWorkspaceHandler(IWorkspaceRepository repository) : IRequestHandler<GetWorkspaceQuery, Workspace?>
{
    public Task<Workspace?> Handle(GetWorkspaceQuery request, CancellationToken cancellationToken)
        => Task.FromResult(repository.GetById(request.Id));
}
