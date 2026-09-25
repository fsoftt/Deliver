using Deliver.SharedKernel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Deliver.ServiceDefaults.Errors;

/// <summary>Translates domain/application exceptions into RFC 9457 problem details.</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title)? mapping = exception switch
        {
            InvalidValueException => (StatusCodes.Status400BadRequest, "Invalid value"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            _ => null,
        };

        if (mapping is null)
            return false;

        httpContext.Response.StatusCode = mapping.Value.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapping.Value.Status,
                Title = mapping.Value.Title,
                Detail = exception.Message,
            },
        });
    }
}
