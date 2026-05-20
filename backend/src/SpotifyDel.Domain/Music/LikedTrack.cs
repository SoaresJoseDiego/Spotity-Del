namespace SpotifyDel.Domain.Music;

public sealed record LikedTrack(Track Track, DateTimeOffset AddedAt);
