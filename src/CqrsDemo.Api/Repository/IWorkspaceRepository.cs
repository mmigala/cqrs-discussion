using CqrsDemo.Api.Domain;

namespace CqrsDemo.Api.Repository;

public interface IWorkspaceRepository
{
    Workspace? GetById(Guid id);
    Workspace Create(string name);
    IReadOnlyList<Workspace> GetAll();
}
