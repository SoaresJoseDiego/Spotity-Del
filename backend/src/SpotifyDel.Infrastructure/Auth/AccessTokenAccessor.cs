using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SpotifyDel.Application.Abstractions;

namespace SpotifyDel.Infrastructure.Auth;

public sealed class AccessTokenAccessor(
    ISessionRepository sessions,
    ISpotifyAuthClient authClient,
    ITokenProtector protector,
    TimeProvider clock,
    ILogger<AccessTokenAccessor> logger) : IAccessTokenAccessor
{
    private static readonly TimeSpan Skew = TimeSpan.FromSeconds(60);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RefreshLocks = new();

    public async Task<string> GetValidAccessTokenAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(sessionId, ct)
            ?? throw new UnauthorizedAccessException("Session not found.");

        var tokens = session.Tokens
            ?? throw new InvalidOperationException("Session has no tokens.");

        if (!tokens.IsExpired(clock, Skew))
            return protector.Unprotect(tokens.AccessTokenEncrypted);

        var gate = RefreshLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            session = await sessions.GetByIdAsync(sessionId, ct)
                ?? throw new UnauthorizedAccessException("Session disappeared during refresh.");
            tokens = session.Tokens!;

            if (!tokens.IsExpired(clock, Skew))
                return protector.Unprotect(tokens.AccessTokenEncrypted);

            var refreshToken = protector.Unprotect(tokens.RefreshTokenEncrypted);
            logger.LogInformation("Refreshing Spotify access token for session {SessionId}", sessionId);

            var fresh = await authClient.RefreshTokenAsync(refreshToken, ct);

            tokens.AccessTokenEncrypted = protector.Protect(fresh.AccessToken);
            tokens.ExpiresAt = clock.GetUtcNow().AddSeconds(fresh.ExpiresInSeconds);
            if (fresh.RefreshToken is { Length: > 0 })
                tokens.RefreshTokenEncrypted = protector.Protect(fresh.RefreshToken);
            if (!string.IsNullOrWhiteSpace(fresh.Scope))
                tokens.Scopes = fresh.Scope;

            await sessions.UpdateAsync(session, ct);
            return fresh.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }
}
