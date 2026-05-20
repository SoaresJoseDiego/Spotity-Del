using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;
using Artist = SpotifyDel.Domain.Music.Artist;

namespace SpotifyDel.Application.Abstractions;

public interface ISpotifyClient
{
    Task<SpotifyUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken ct);

    Task<Page<LikedTrack>> GetLikedTracksAsync(
        string accessToken,
        int offset,
        int limit,
        CancellationToken ct);

    Task RemoveLikedTracksAsync(
        string accessToken,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct);

    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetArtistGenresAsync(
        string accessToken,
        IReadOnlyCollection<string> artistIds,
        CancellationToken ct);

    Task<Page<Playlist>> GetUserPlaylistsAsync(
        string accessToken,
        int offset,
        int limit,
        CancellationToken ct);

    Task<Page<PlaylistTrack>> GetPlaylistTracksAsync(
        string accessToken,
        string playlistId,
        int offset,
        int limit,
        CancellationToken ct);

    Task RemovePlaylistTracksAsync(
        string accessToken,
        string playlistId,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct);

    Task<IReadOnlyList<RecentPlay>> GetRecentlyPlayedAsync(
        string accessToken,
        CancellationToken ct);

    Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
        string accessToken,
        string timeRange,
        int limit,
        CancellationToken ct);

    Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
        string accessToken,
        string timeRange,
        int limit,
        CancellationToken ct);
}

public sealed record RecentPlay(
    string TrackId,
    string TrackName,
    string AlbumName,
    string? AlbumImageUrl,
    IReadOnlyList<Artist> Artists,
    string ExternalUrl,
    DateTimeOffset PlayedAt);

public sealed record TopArtist(
    string Id,
    string Name,
    string? ImageUrl,
    IReadOnlyList<string> Genres,
    int Popularity,
    int Followers);

public sealed record TopTrack(
    string Id,
    string Name,
    int DurationMs,
    string ExternalUrl,
    string AlbumName,
    string? AlbumImageUrl,
    IReadOnlyList<Artist> Artists,
    int Popularity);

public sealed record SpotifyUserProfile(
    string Id,
    string DisplayName,
    string? AvatarUrl,
    string? Email);
