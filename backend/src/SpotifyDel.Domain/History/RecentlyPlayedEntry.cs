namespace SpotifyDel.Domain.History;

public sealed class RecentlyPlayedEntry
{
    public required Guid Id { get; init; }
    public required Guid UserSessionId { get; init; }
    public required string TrackId { get; init; }
    public required DateTimeOffset PlayedAt { get; init; }
}
