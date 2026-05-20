namespace SpotifyDel.Domain.Sessions;

public sealed class SpotifyTokens
{
    public required Guid UserSessionId { get; init; }
    public required string AccessTokenEncrypted { get; set; }
    public required string RefreshTokenEncrypted { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public string Scopes { get; set; } = string.Empty;

    public bool IsExpired(TimeProvider clock, TimeSpan skew) =>
        clock.GetUtcNow() >= ExpiresAt - skew;
}
