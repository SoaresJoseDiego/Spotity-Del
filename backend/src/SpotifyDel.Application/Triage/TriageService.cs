using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Application.Triage;

public sealed record TriagePlaylistOccurrence(string PlaylistId, string PlaylistName, bool CanEdit);

public sealed record TriageTrack(
    string Id,
    string Name,
    string? ImageUrl,
    int DurationMs,
    string ExternalUrl,
    IReadOnlyList<Artist> Artists,
    DateTimeOffset? LikedAt,
    IReadOnlyList<TriagePlaylistOccurrence> InPlaylists)
{
    public bool InLiked => LikedAt is not null;
    public int OriginsCount => (InLiked ? 1 : 0) + InPlaylists.Count;
}

public sealed record LibrarySnapshot(
    int LikedCount,
    int PlaylistCount,
    IReadOnlyList<TriageTrack> Tracks);

public sealed record TriageRemoval(
    string TrackId,
    bool RemoveFromLiked,
    IReadOnlyList<string> RemoveFromPlaylistIds);

public sealed record TriageRemovalResult(int LikedRemoved, int PlaylistTracksRemoved, IReadOnlyList<string> Failures);

public sealed class TriageService(
    ISpotifyClient spotify,
    IAccessTokenAccessor tokens,
    ISessionRepository sessions)
{
    private const int LikedPageSize    = 50;
    private const int PlaylistPageSize = 100;
    private const int PlaylistListPage = 50;

    public async Task<LibrarySnapshot> ScanAsync(Guid sessionId, CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);
        var session = await sessions.GetByIdAsync(sessionId, ct)
            ?? throw new UnauthorizedAccessException("Session not found.");

        var tracks = new Dictionary<string, TriageTrackAccumulator>(StringComparer.Ordinal);

        // 1. Liked tracks
        var likedOffset = 0;
        while (true)
        {
            var page = await spotify.GetLikedTracksAsync(token, likedOffset, LikedPageSize, ct);
            foreach (var liked in page.Items)
            {
                var t = liked.Track;
                if (string.IsNullOrEmpty(t.Id)) continue;
                var acc = GetOrAdd(tracks, t);
                acc.LikedAt = liked.AddedAt;
            }
            if (!page.HasMore) break;
            likedOffset += page.Items.Count;
            if (page.Items.Count == 0) break;
        }
        var likedCount = tracks.Count;

        // 2. Playlists owned or collaborated on (skip read-only).
        var playlists = new List<Playlist>();
        var plOffset = 0;
        while (true)
        {
            var page = await spotify.GetUserPlaylistsAsync(token, plOffset, PlaylistListPage, ct);
            foreach (var p in page.Items)
            {
                var canEdit = p.OwnerId == session.SpotifyUserId || p.IsCollaborative;
                if (canEdit) playlists.Add(p);
            }
            if (!page.HasMore) break;
            plOffset += page.Items.Count;
            if (page.Items.Count == 0) break;
        }

        // 3. For each editable playlist, list its tracks.
        foreach (var p in playlists)
        {
            var ref_ = new TriagePlaylistOccurrence(p.Id, p.Name, CanEdit: true);
            var offset = 0;
            while (true)
            {
                var page = await spotify.GetPlaylistTracksAsync(token, p.Id, offset, PlaylistPageSize, ct);
                foreach (var pt in page.Items)
                {
                    var t = pt.Track;
                    if (string.IsNullOrEmpty(t.Id)) continue;
                    var acc = GetOrAdd(tracks, t);
                    acc.Playlists.Add(ref_);
                }
                if (!page.HasMore) break;
                offset += page.Items.Count;
                if (page.Items.Count == 0) break;
            }
        }

        var result = tracks.Values
            .Select(a => new TriageTrack(
                a.Id, a.Name, a.ImageUrl, a.DurationMs, a.ExternalUrl,
                a.Artists, a.LikedAt, a.Playlists))
            .OrderByDescending(t => t.OriginsCount)
            .ThenBy(t => t.Name)
            .ToList();

        return new LibrarySnapshot(likedCount, playlists.Count, result);
    }

    public async Task<TriageRemovalResult> ExecuteAsync(
        Guid sessionId,
        IReadOnlyList<TriageRemoval> removals,
        CancellationToken ct)
    {
        if (removals.Count == 0) return new TriageRemovalResult(0, 0, []);

        var likedIds = removals.Where(r => r.RemoveFromLiked).Select(r => r.TrackId).ToList();
        var byPlaylist = removals
            .SelectMany(r => r.RemoveFromPlaylistIds.Select(pid => (PlaylistId: pid, r.TrackId)))
            .GroupBy(x => x.PlaylistId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TrackId).ToList());

        var failures = new List<string>();
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);

        var likedRemoved = 0;
        if (likedIds.Count > 0)
        {
            const int batch = 40;
            foreach (var chunk in likedIds.Chunk(batch))
            {
                try
                {
                    await spotify.RemoveLikedTracksAsync(token, chunk, ct);
                    likedRemoved += chunk.Length;
                }
                catch (Exception ex)
                {
                    failures.Add($"Curtidas: {ex.Message}");
                }
            }
        }

        var playlistRemoved = 0;
        foreach (var (playlistId, trackIds) in byPlaylist)
        {
            const int batch = 100;
            foreach (var chunk in trackIds.Chunk(batch))
            {
                try
                {
                    await spotify.RemovePlaylistTracksAsync(token, playlistId, chunk, ct);
                    playlistRemoved += chunk.Length;
                }
                catch (Exception ex)
                {
                    failures.Add($"Playlist {playlistId}: {ex.Message}");
                }
            }
        }

        return new TriageRemovalResult(likedRemoved, playlistRemoved, failures);
    }

    private static TriageTrackAccumulator GetOrAdd(
        Dictionary<string, TriageTrackAccumulator> map,
        Track t)
    {
        if (!map.TryGetValue(t.Id, out var acc))
        {
            acc = new TriageTrackAccumulator
            {
                Id = t.Id,
                Name = t.Name,
                ImageUrl = t.Album.ImageUrl,
                DurationMs = t.DurationMs,
                ExternalUrl = t.ExternalUrl,
                Artists = t.Artists,
            };
            map[t.Id] = acc;
        }
        return acc;
    }

    private sealed class TriageTrackAccumulator
    {
        public required string Id;
        public required string Name;
        public string? ImageUrl;
        public int DurationMs;
        public required string ExternalUrl;
        public required IReadOnlyList<Artist> Artists;
        public DateTimeOffset? LikedAt;
        public List<TriagePlaylistOccurrence> Playlists { get; } = new();
    }
}
