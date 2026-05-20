using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyDel.Api.Auth;
using SpotifyDel.Application.Triage;

namespace SpotifyDel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/triage")]
public sealed class TriageController(TriageService triage) : ControllerBase
{
    [HttpPost("scan")]
    public async Task<IActionResult> Scan(CancellationToken ct)
    {
        var snapshot = await triage.ScanAsync(User.RequireSessionId(), ct);
        return Ok(new
        {
            likedCount = snapshot.LikedCount,
            playlistCount = snapshot.PlaylistCount,
            tracks = snapshot.Tracks.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                imageUrl = t.ImageUrl,
                durationMs = t.DurationMs,
                externalUrl = t.ExternalUrl,
                artists = t.Artists.Select(a => new { id = a.Id, name = a.Name }),
                likedAt = t.LikedAt,
                inLiked = t.InLiked,
                inPlaylists = t.InPlaylists.Select(p => new
                {
                    id = p.PlaylistId,
                    name = p.PlaylistName,
                    canEdit = p.CanEdit,
                }),
                originsCount = t.OriginsCount,
            }),
        });
    }

    public sealed record RemovalItem(string TrackId, bool RemoveFromLiked, IReadOnlyList<string> RemoveFromPlaylistIds);
    public sealed record RemovalRequest(IReadOnlyList<RemovalItem> Items);

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] RemovalRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { error = "items_required" });

        var removals = request.Items
            .Select(i => new TriageRemoval(i.TrackId, i.RemoveFromLiked, i.RemoveFromPlaylistIds ?? []))
            .ToList();

        var result = await triage.ExecuteAsync(User.RequireSessionId(), removals, ct);
        return Ok(new
        {
            likedRemoved = result.LikedRemoved,
            playlistTracksRemoved = result.PlaylistTracksRemoved,
            failures = result.Failures,
        });
    }
}
