using Microsoft.Extensions.Logging;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Application.Auth;

public sealed class AuthService(
    ISpotifyAuthClient authClient,
    ISpotifyClient spotifyClient,
    ISessionRepository sessions,
    ITokenProtector protector,
    TimeProvider clock,
    ILogger<AuthService> logger)
{
    public async Task<UserSession> CompleteLoginAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        var tokens = await authClient.ExchangeCodeAsync(code, codeVerifier, redirectUri, ct);
        if (tokens.RefreshToken is null)
            throw new InvalidOperationException("Spotify did not return a refresh token.");

        logger.LogInformation("Spotify granted scopes: {Scopes}", tokens.Scope);

        var profile = await spotifyClient.GetCurrentUserAsync(tokens.AccessToken, ct);

        var now = clock.GetUtcNow();
        var existing = await sessions.GetBySpotifyUserIdAsync(profile.Id, ct);

        if (existing is not null)
        {
            existing.DisplayName = profile.DisplayName;
            existing.AvatarUrl = profile.AvatarUrl;
            existing.LastSeenAt = now;
            existing.Tokens = new SpotifyTokens
            {
                UserSessionId = existing.Id,
                AccessTokenEncrypted = protector.Protect(tokens.AccessToken),
                RefreshTokenEncrypted = protector.Protect(tokens.RefreshToken),
                ExpiresAt = now.AddSeconds(tokens.ExpiresInSeconds),
                Scopes = tokens.Scope,
            };
            await sessions.UpdateAsync(existing, ct);
            logger.LogInformation("Refreshed session for Spotify user {SpotifyUserId}", profile.Id);
            return existing;
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            SpotifyUserId = profile.Id,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = now,
            LastSeenAt = now,
        };
        session.Tokens = new SpotifyTokens
        {
            UserSessionId = session.Id,
            AccessTokenEncrypted = protector.Protect(tokens.AccessToken),
            RefreshTokenEncrypted = protector.Protect(tokens.RefreshToken),
            ExpiresAt = now.AddSeconds(tokens.ExpiresInSeconds),
            Scopes = tokens.Scope,
        };
        await sessions.AddAsync(session, ct);
        logger.LogInformation("Created session for Spotify user {SpotifyUserId}", profile.Id);
        return session;
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken ct)
    {
        await sessions.DeleteAsync(sessionId, ct);
    }
}
