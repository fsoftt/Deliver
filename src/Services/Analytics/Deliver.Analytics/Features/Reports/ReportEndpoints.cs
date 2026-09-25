using Deliver.Analytics.Data;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Analytics.Features.Reports;

public sealed record ShipmentStatistics(int Total, IReadOnlyDictionary<string, int> ByStatus, IReadOnlyList<DailyCount> PerDay);

public sealed record DailyCount(DateOnly Date, int Shipments);

public sealed record DeliveryStatistics(
    int Delivered,
    int Cancelled,
    double? AverageMinutesToAssign,
    double? AverageMinutesToDeliver,
    decimal RevenueCaptured,
    int PaymentFailures);

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/shipments", async (AnalyticsDbContext db, TimeProvider clock, CancellationToken ct) =>
            {
                var facts = db.ShipmentFacts.AsNoTracking();

                var byStatus = await facts
                    .GroupBy(f => f.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

                var since = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime.AddDays(-30));
                var perDay = await facts
                    .Where(f => f.CreatedOn >= since)
                    .GroupBy(f => f.CreatedOn!.Value)
                    .Select(g => new DailyCount(g.Key, g.Count()))
                    .OrderBy(d => d.Date)
                    .ToListAsync(ct);

                return TypedResults.Ok(new ShipmentStatistics(byStatus.Values.Sum(), byStatus, perDay));
            })
            .WithName("GetShipmentStatistics");

        group.MapGet("/deliveries", async (AnalyticsDbContext db, CancellationToken ct) =>
            {
                var facts = db.ShipmentFacts.AsNoTracking();

                var statistics = new DeliveryStatistics(
                    Delivered: await facts.CountAsync(f => f.DeliveredAt != null, ct),
                    Cancelled: await facts.CountAsync(f => f.CancelledAt != null, ct),
                    AverageMinutesToAssign: await facts.Where(f => f.MinutesToAssign != null).AverageAsync(f => f.MinutesToAssign, ct),
                    AverageMinutesToDeliver: await facts.Where(f => f.MinutesToDeliver != null).AverageAsync(f => f.MinutesToDeliver, ct),
                    RevenueCaptured: await facts.SumAsync(f => f.Revenue ?? 0, ct),
                    PaymentFailures: await facts.CountAsync(f => f.PaymentFailed, ct));

                return TypedResults.Ok(statistics);
            })
            .WithName("GetDeliveryStatistics");

        return app;
    }
}
