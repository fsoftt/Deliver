using Deliver.Notifications.Data;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Notifications.Features.History;

public sealed record NotificationView(Guid Id, Guid ShipmentId, Guid CustomerId, string Channel, string Message, string TriggeredBy, DateTimeOffset SentAt);

public static class NotificationHistoryEndpoint
{
    public static IEndpointRouteBuilder MapNotificationHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (NotificationsDbContext db, Guid? shipmentId, Guid? customerId, CancellationToken ct) =>
            {
                var query = db.Notifications.AsNoTracking();
                if (shipmentId is { } s)
                    query = query.Where(n => n.ShipmentId == s);
                if (customerId is { } c)
                    query = query.Where(n => n.CustomerId == c);

                var notifications = await query
                    .OrderByDescending(n => n.SentAt)
                    .Take(100)
                    .Select(n => new NotificationView(n.Id, n.ShipmentId, n.CustomerId, n.Channel.ToString(), n.Message, n.TriggeredBy, n.SentAt))
                    .ToListAsync(ct);

                return TypedResults.Ok(notifications);
            })
            .WithTags("Notifications")
            .WithName("ListNotifications");

        return app;
    }
}
