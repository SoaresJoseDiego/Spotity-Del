using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Api.Contracts;

public sealed record PlaylistDto(
    string Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string OwnerId,
    string OwnerName,
    int TrackCount,
    bool IsCollaborative,
    bool IsPublic,
    bool CanEdit);

public static class PlaylistContracts
{
    public static PlaylistDto Map(Playlist p, string currentUserSpotifyId) => new(
        p.Id,
        p.Name,
        p.Description,
        p.ImageUrl,
        p.OwnerId,
        p.OwnerName,
        p.TrackCount,
        p.IsCollaborative,
        p.IsPublic,
        CanEdit: p.OwnerId == currentUserSpotifyId || p.IsCollaborative);

    public static PageDto<PlaylistDto> MapPage(Page<Playlist> page, string currentUserSpotifyId) => new(
        page.Items.Select(p => Map(p, currentUserSpotifyId)).ToList(),
        page.Offset,
        page.Limit,
        page.Total,
        page.HasMore);

    public static LikedTrackDto MapPlaylistTrack(PlaylistTrack pt) =>
        LikedTrackContracts.Map(new LikedTrack(pt.Track, pt.AddedAt));

    public static PageDto<LikedTrackDto> MapTrackPage(Page<PlaylistTrack> page) => new(
        page.Items.Select(MapPlaylistTrack).ToList(),
        page.Offset,
        page.Limit,
        page.Total,
        page.HasMore);
}
