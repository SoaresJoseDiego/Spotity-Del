using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Api.Contracts;

public sealed record RemoveTracksRequest(IReadOnlyList<string> Ids);

public sealed record FilterRequestDto(
    DateTimeOffset? AddedBefore,
    int? MinArtistOccurrences,
    IReadOnlyList<string>? ExcludeGenres);

public sealed record LikedTrackDto(
    string Id,
    string Name,
    int DurationMs,
    string? PreviewUrl,
    string ExternalUrl,
    DateTimeOffset AddedAt,
    AlbumDto Album,
    IReadOnlyList<ArtistDto> Artists);

public sealed record AlbumDto(string Id, string Name, string? ImageUrl);
public sealed record ArtistDto(string Id, string Name);

public sealed record PageDto<T>(IReadOnlyList<T> Items, int Offset, int Limit, int Total, bool HasMore);

public static class LikedTrackContracts
{
    public static LikedTrackDto Map(LikedTrack liked) => new(
        liked.Track.Id,
        liked.Track.Name,
        liked.Track.DurationMs,
        liked.Track.PreviewUrl,
        liked.Track.ExternalUrl,
        liked.AddedAt,
        new AlbumDto(liked.Track.Album.Id, liked.Track.Album.Name, liked.Track.Album.ImageUrl),
        liked.Track.Artists.Select(a => new ArtistDto(a.Id, a.Name)).ToList());

    public static PageDto<LikedTrackDto> MapPage(Page<LikedTrack> page) => new(
        page.Items.Select(Map).ToList(),
        page.Offset,
        page.Limit,
        page.Total,
        page.HasMore);
}
