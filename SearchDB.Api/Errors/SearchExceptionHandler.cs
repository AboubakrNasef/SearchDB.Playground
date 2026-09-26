using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SearchDB.Application.Search;

namespace SearchDB.Api.Errors;

public sealed class SearchExceptionHandler(ILogger<SearchExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var isUnavailable = exception is SearchProviderUnavailableException;
        var statusCode = isUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        if (isUnavailable)
            logger.LogWarning(exception, "Search provider unavailable");
        else
            logger.LogError(exception, "Unexpected search request failure");

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = isUnavailable ? "Search provider unavailable" : "Search request failed",
            Detail = isUnavailable
                ? "The selected search provider is temporarily unavailable. Check its connection and search index configuration."
                : "An unexpected error occurred while processing the search request."
        }, cancellationToken);
        return true;
    }
}
