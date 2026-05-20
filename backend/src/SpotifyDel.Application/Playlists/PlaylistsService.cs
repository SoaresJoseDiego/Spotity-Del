using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Application.Playlists;

public sealed class PlaylistsService(
    ISpotifyClient spotify,
    IAccessTokenAccessor tokens)
{
    public async Task<Page<Playlist>> GetUserPlaylistsAsync(
        Guid sessionId,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);
        return await spotify.GetUserPlaylistsAsync(token, offset, limit, ct);
    }

    public async Task<Page<PlaylistTrack>> GetTracksAsync(
        Guid sessionId,
        string playlistId,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);
        return await spotify.GetPlaylistTracksAsync(token, playlistId, offset, limit, ct);
    }

    public async Task RemoveTracksAsync(
        Guid sessionId,
        string playlistId,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct)
    {
        if (trackIds.Count == 0) return;

        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);

        const int spotifyBatchSize = 100;
        foreach (var batch in trackIds.Chunk(spotifyBatchSize))
        {
            await spotify.RemovePlaylistTracksAsync(token, playlistId, batch, ct);
        }
    }
}
