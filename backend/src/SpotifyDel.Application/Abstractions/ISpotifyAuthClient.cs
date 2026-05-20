namespace SpotifyDel.Application.Abstractions;

public interface ISpotifyAuthClient
{
    Task<SpotifyTokenResponse> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct);

    Task<SpotifyTokenResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct);
}

public sealed record SpotifyTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds,
    string Scope);
