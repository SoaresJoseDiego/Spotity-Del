using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Application.Tracks;

public sealed class TracksService(
    ISpotifyClient spotify,
    IAccessTokenAccessor tokens)
{
    public async Task<Page<LikedTrack>> GetLikedAsync(
        Guid sessionId,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);
        return await spotify.GetLikedTracksAsync(token, offset, limit, ct);
    }

    public async Task RemoveAsync(
        Guid sessionId,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct)
    {
        if (trackIds.Count == 0) return;

        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);

        // Feb/2026 migration: /me/library accepts max 40 per request (was 50 on /me/tracks).
        const int spotifyBatchSize = 40;
        foreach (var batch in trackIds.Chunk(spotifyBatchSize))
        {
            await spotify.RemoveLikedTracksAsync(token, batch, ct);
        }
    }
}
