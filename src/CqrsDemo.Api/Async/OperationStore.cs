using System.Collections.Concurrent;

namespace CqrsDemo.Api.Async;

public record Operation(Guid Id, string Status, Guid? ResourceId, string? Error, DateTime CreatedAt);

public class OperationStore
{
    private readonly ConcurrentDictionary<Guid, Operation> _store = new();

    public Operation Create()
    {
        var op = new Operation(Guid.NewGuid(), "Pending", null, null, DateTime.UtcNow);
        _store[op.Id] = op;
        return op;
    }

    public void Complete(Guid operationId, Guid resourceId)
        => _store[operationId] = _store[operationId] with { Status = "Completed", ResourceId = resourceId };

    public void Fail(Guid operationId, string error)
        => _store[operationId] = _store[operationId] with { Status = "Failed", Error = error };

    public Operation? GetById(Guid id) => _store.GetValueOrDefault(id);
}
