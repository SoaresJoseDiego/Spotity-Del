namespace SpotifyDel.Domain.Sessions;

public sealed class UserSession
{
    public required Guid Id { get; init; }
    public required string SpotifyUserId { get; init; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; set; }

    public SpotifyTokens? Tokens { get; set; }

    public void Touch(TimeProvider clock) => LastSeenAt = clock.GetUtcNow();
}
