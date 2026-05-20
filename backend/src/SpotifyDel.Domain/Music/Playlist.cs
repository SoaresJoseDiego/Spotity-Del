namespace SpotifyDel.Domain.Music;

public sealed record Playlist(
    string Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string OwnerId,
    string OwnerName,
    int TrackCount,
    bool IsCollaborative,
    bool IsPublic);

public sealed record PlaylistTrack(Track Track, DateTimeOffset AddedAt);
