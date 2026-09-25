namespace Deliver.Messaging;

/// <summary>
/// Ambient correlation id for the current logical flow (HTTP request or consumed message).
/// Written by the HTTP middleware / message consumer, read by the outbox.
/// </summary>
public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-Id";

    private static readonly AsyncLocal<string?> Current_ = new();

    public static string? Current
    {
        get => Current_.Value;
        set => Current_.Value = value;
    }
}
