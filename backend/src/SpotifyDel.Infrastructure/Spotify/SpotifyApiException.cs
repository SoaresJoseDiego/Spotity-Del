using System.Net;

namespace SpotifyDel.Infrastructure.Spotify;

public sealed class SpotifyApiException(
    HttpStatusCode statusCode,
    string spotifyMessage,
    string endpoint)
    : Exception($"Spotify {endpoint} returned {(int)statusCode}: {spotifyMessage}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string SpotifyMessage { get; } = spotifyMessage;
    public string Endpoint { get; } = endpoint;
}
