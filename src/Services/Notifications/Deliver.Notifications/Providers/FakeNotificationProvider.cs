using System.Collections.Concurrent;
using Deliver.Notifications.Data;
using Microsoft.Extensions.Options;

namespace Deliver.Notifications.Providers;

public sealed record OutgoingNotification(Guid CustomerId, NotificationChannel Channel, string Message);

/// <summary>Port to an email/SMS/push provider.</summary>
public interface INotificationProvider
{
    /// <returns>The provider's message id.</returns>
    Task<string> SendAsync(OutgoingNotification notification, CancellationToken cancellationToken);
}

public sealed class FakeNotificationProviderOptions
{
    public const string SectionName = "Notifications:FakeProvider";

    /// <summary>Always fails for these customers: the message ends in the dead-letter queue.</summary>
    public List<Guid> UnreachableCustomerIds { get; set; } = [];

    /// <summary>Fails the first <see cref="FlakyFailuresBeforeSuccess"/> attempts for these customers: shows retries.</summary>
    public List<Guid> FlakyCustomerIds { get; set; } = [];

    public int FlakyFailuresBeforeSuccess { get; set; } = 2;
}

public sealed class NotificationProviderUnavailableException(string message) : Exception(message);

/// <summary>
/// Real providers are out of scope. This fake one "sends" by logging and can simulate transient and
/// permanent outages so retry and dead-letter behaviour can be demonstrated deterministically.
/// </summary>
internal sealed class FakeNotificationProvider(
    IOptions<FakeNotificationProviderOptions> options,
    ILogger<FakeNotificationProvider> logger) : INotificationProvider
{
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public Task<string> SendAsync(OutgoingNotification notification, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (settings.UnreachableCustomerIds.Contains(notification.CustomerId))
            throw new NotificationProviderUnavailableException($"Customer {notification.CustomerId} is unreachable.");

        if (settings.FlakyCustomerIds.Contains(notification.CustomerId))
        {
            var attempt = _attempts.AddOrUpdate($"{notification.CustomerId}:{notification.Message}", 1, (_, n) => n + 1);
            if (attempt <= settings.FlakyFailuresBeforeSuccess)
                throw new NotificationProviderUnavailableException($"Provider timeout (simulated, attempt {attempt}).");
        }

        logger.LogInformation("[{Channel}] to customer {CustomerId}: {Message}",
            notification.Channel, notification.CustomerId, notification.Message);

        return Task.FromResult($"fake-{Guid.NewGuid():N}");
    }
}
