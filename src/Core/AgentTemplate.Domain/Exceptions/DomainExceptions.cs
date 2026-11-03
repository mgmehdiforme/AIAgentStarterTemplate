namespace AgentTemplate.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class AgentLoopExhaustedException : DomainException
{
    public int MaxIterations { get; }

    public AgentLoopExhaustedException(string message, int maxIterations = 5) : base(message)
    {
        MaxIterations = maxIterations;
    }
}

public class UnauthorizedOperationException : DomainException
{
    public string Operation { get; }
    public string UserId { get; }

    public UnauthorizedOperationException(string operation, string userId, string message) : base(message)
    {
        Operation = operation;
        UserId = userId;
    }
}

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key) 
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
