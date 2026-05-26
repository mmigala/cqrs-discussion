using System.Collections.Concurrent;
using CqrsDemo.Api.Domain;

namespace CqrsDemo.Api.Repository;

public class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly ConcurrentDictionary<Guid, Workspace> _store = new();

    public Workspace? GetById(Guid id) => _store.GetValueOrDefault(id);

    public Workspace Create(string name)
    {
        var workspace = new Workspace(Guid.NewGuid(), name, DateTime.UtcNow);
        _store[workspace.Id] = workspace;
        return workspace;
    }

    public IReadOnlyList<Workspace> GetAll() => _store.Values.ToList();
}
