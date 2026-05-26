using CqrsDemo.Api.Domain;
using CqrsDemo.Api.Repository;

namespace CqrsDemo.Api.Services;

public class WorkspaceService(IWorkspaceRepository repository) : IWorkspaceService
{
    public Workspace Create(string name) => repository.Create(name);
    public Workspace? GetById(Guid id) => repository.GetById(id);
}
