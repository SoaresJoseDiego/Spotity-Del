using Microsoft.AspNetCore.Diagnostics;
using SpotifyDel.Infrastructure.Spotify;

namespace SpotifyDel.Api.ErrorHandling;

public sealed class SpotifyExceptionHandler(ILogger<SpotifyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not SpotifyApiException ex) return false;

        logger.LogWarning(
            "Spotify API {Endpoint} → {Status}: {Message}",
            ex.Endpoint, (int)ex.StatusCode, ex.SpotifyMessage);

        httpContext.Response.StatusCode = (int)ex.StatusCode switch
        {
            401 => StatusCodes.Status401Unauthorized,
            403 => StatusCodes.Status403Forbidden,
            404 => StatusCodes.Status404NotFound,
            429 => StatusCodes.Status429TooManyRequests,
            _   => StatusCodes.Status502BadGateway,
        };

        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = "spotify_api_error",
            status = (int)ex.StatusCode,
            message = ex.SpotifyMessage,
            endpoint = ex.Endpoint,
        }, cancellationToken);

        return true;
    }
}
