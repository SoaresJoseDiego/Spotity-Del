using SpotifyDel.Application.Abstractions;
using SpotifyDel.Application.Tracks;
using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Tests.Tracks;

public class FilterServiceTests
{
    [Fact]
    public async Task Marks_tracks_added_before_cutoff()
    {
        var sessionId = Guid.NewGuid();
        var cutoff = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var old = Liked("old", addedAt: cutoff.AddYears(-1));
        var recent = Liked("recent", addedAt: cutoff.AddDays(10));

        var spotify = new FakeSpotifyClient([old, recent]);
        var tokens = new FakeTokenAccessor();
        var service = new FilterService(spotify, tokens);

        var matches = await service.EvaluateAsync(
            sessionId,
            new FilterRequest(AddedBefore: cutoff, MinArtistOccurrences: null, ExcludeGenres: null),
            CancellationToken.None);

        var ids = matches.Select(m => m.Track.Track.Id).ToList();
        Assert.Equal(["old"], ids);
        Assert.Contains("adicionada antes", matches[0].Reasons[0]);
    }

    [Fact]
    public async Task Marks_tracks_whose_artist_repeats()
    {
        var artistA = new Artist("a1", "Solo Repeated");
        var artistB = new Artist("a2", "Solo Unique");

        var t1 = Liked("t1", artists: [artistA]);
        var t2 = Liked("t2", artists: [artistA]);
        var t3 = Liked("t3", artists: [artistA]);
        var t4 = Liked("t4", artists: [artistB]);

        var spotify = new FakeSpotifyClient([t1, t2, t3, t4]);
        var service = new FilterService(spotify, new FakeTokenAccessor());

        var matches = await service.EvaluateAsync(
            Guid.NewGuid(),
            new FilterRequest(null, MinArtistOccurrences: 3, null),
            CancellationToken.None);

        Assert.Equal(["t1", "t2", "t3"], matches.Select(m => m.Track.Track.Id).Order());
    }

    [Fact]
    public async Task Combines_multiple_reasons_per_track()
    {
        var artist = new Artist("a", "Repeated Artist");
        var oldRepeated = Liked("a1", artists: [artist], addedAt: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var oldOther    = Liked("a2", artists: [artist], addedAt: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var service = new FilterService(new FakeSpotifyClient([oldRepeated, oldOther]), new FakeTokenAccessor());

        var matches = await service.EvaluateAsync(
            Guid.NewGuid(),
            new FilterRequest(
                AddedBefore: new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                MinArtistOccurrences: 2,
                ExcludeGenres: null),
            CancellationToken.None);

        Assert.All(matches, m => Assert.Equal(2, m.Reasons.Count));
    }

    private static LikedTrack Liked(
        string id,
        DateTimeOffset? addedAt = null,
        IReadOnlyList<Artist>? artists = null)
    {
        var track = new Track(
            id, $"Track {id}", 180_000, null, $"https://open.spotify.com/track/{id}",
            new Album($"album-{id}", $"Album {id}", null),
            artists ?? [new Artist($"artist-{id}", $"Artist {id}")]);
        return new LikedTrack(track, addedAt ?? DateTimeOffset.UtcNow);
    }

    private sealed class FakeSpotifyClient(IReadOnlyList<LikedTrack> tracks) : ISpotifyClient
    {
        public Task<Page<LikedTrack>> GetLikedTracksAsync(
            string accessToken, int offset, int limit, CancellationToken ct)
        {
            var slice = tracks.Skip(offset).Take(limit).ToList();
            return Task.FromResult(new Page<LikedTrack>(slice, offset, limit, tracks.Count));
        }

        public Task<SpotifyUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task RemoveLikedTracksAsync(string accessToken, IReadOnlyCollection<string> ids, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetArtistGenresAsync(
            string accessToken, IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>>());

        public Task<Page<Playlist>> GetUserPlaylistsAsync(
            string accessToken, int offset, int limit, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<Page<PlaylistTrack>> GetPlaylistTracksAsync(
            string accessToken, string playlistId, int offset, int limit, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task RemovePlaylistTracksAsync(
            string accessToken, string playlistId, IReadOnlyCollection<string> ids, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<RecentPlay>> GetRecentlyPlayedAsync(
            string accessToken, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RecentPlay>>([]);
        public Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
            string accessToken, string timeRange, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<TopArtist>>([]);
        public Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
            string accessToken, string timeRange, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<TopTrack>>([]);
    }

    private sealed class FakeTokenAccessor : IAccessTokenAccessor
    {
        public Task<string> GetValidAccessTokenAsync(Guid sessionId, CancellationToken ct) =>
            Task.FromResult("fake-token");
    }
}
