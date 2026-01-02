namespace ChoreNotifier.Models;

using FluentResults;

public abstract class DomainError : Error
{
    public string Code { get; }

    protected DomainError(string code, string message)
        : base(message)
    {
        Code = code;
        Metadata["code"] = code;
    }
}

public sealed class NotFoundError : DomainError
{
    public string EntityName { get; }
    public object EntityKey { get; }

    public NotFoundError(string entityName, object entityKey)
        : base(
            code: "not_found",
            message: $"{entityName} with id '{entityKey}' was not found."
        )
    {
        EntityName = entityName;
        EntityKey = entityKey;

        Metadata["entity"] = entityName;
        Metadata["id"] = entityKey;
    }
}


public sealed class ConflictError : DomainError
{
    public ConflictError(string message)
        : base("conflict", message)
    {
    }
}

public sealed class InvalidOperationError : DomainError
{
    public InvalidOperationError(string message)
        : base("invalid_operation", message)
    {
    }
}

public sealed class ValidationError : DomainError
{
    public ValidationError(string message) : base("validation", message) { }
}
