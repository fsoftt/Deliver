using System.Diagnostics;
using Deliver.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Deliver.ServiceDefaults.Correlation;

/// <summary>
/// Accepts an incoming <c>X-Correlation-Id</c> (or creates one), echoes it in the response, adds it to
/// the log scope and trace, and makes it ambient so the outbox stamps it on every event this request causes.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationContext.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100)
            correlationId = Guid.NewGuid().ToString();

        CorrelationContext.Current = correlationId;
        context.Request.Headers[CorrelationContext.HeaderName] = correlationId; // forwarded by the gateway
        context.Response.Headers[CorrelationContext.HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
