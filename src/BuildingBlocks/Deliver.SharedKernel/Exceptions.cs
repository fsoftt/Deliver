namespace Deliver.SharedKernel;

/// <summary>A business rule was violated given the current state of the model (HTTP 409).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>A value can never be valid, regardless of state (HTTP 400).</summary>
public sealed class InvalidValueException(string message) : DomainException(message);

/// <summary>The requested aggregate does not exist (HTTP 404).</summary>
public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} '{id}' was not found.");
