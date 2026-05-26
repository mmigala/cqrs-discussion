using CqrsDemo.Api.Domain;

namespace CqrsDemo.Api.Services;

public interface IWorkspaceService
{
    Workspace Create(string name);
    Workspace? GetById(Guid id);
}
