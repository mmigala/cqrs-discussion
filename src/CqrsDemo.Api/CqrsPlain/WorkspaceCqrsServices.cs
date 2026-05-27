using CqrsDemo.Api.Domain;
using CqrsDemo.Api.Repository;

namespace CqrsDemo.Api.CqrsPlain;

// Command side — handles writes
public interface IWorkspaceCommandService
{
    Workspace Create(string name);
}

public class WorkspaceCommandService(IWorkspaceRepository repository) : IWorkspaceCommandService
{
    public Workspace Create(string name) => repository.Create(name);
}

// Query side — handles reads
public interface IWorkspaceQueryService
{
    Workspace? GetById(Guid id);
}

public class WorkspaceQueryService(IWorkspaceRepository repository) : IWorkspaceQueryService
{
    public Workspace? GetById(Guid id) => repository.GetById(id);
}
