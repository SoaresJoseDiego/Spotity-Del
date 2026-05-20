using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyDel.Api.Auth;
using SpotifyDel.Api.Contracts;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Application.Playlists;

namespace SpotifyDel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/playlists")]
public sealed class PlaylistsController(
    PlaylistsService playlists,
    ISessionRepository sessions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int offset = 0,
        [FromQuery] int limit  = 50,
        CancellationToken ct   = default)
    {
        if (offset < 0)         return BadRequest(new { error = "offset_negative" });
        if (limit is < 1 or > 50)
            return BadRequest(new { error = "limit_out_of_range", min = 1, max = 50 });

        var sessionId = User.RequireSessionId();
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session is null) return Unauthorized();

        var page = await playlists.GetUserPlaylistsAsync(sessionId, offset, limit, ct);
        return Ok(PlaylistContracts.MapPage(page, session.SpotifyUserId));
    }

    [HttpGet("{id}/tracks")]
    public async Task<IActionResult> Tracks(
        string id,
        [FromQuery] int offset = 0,
        [FromQuery] int limit  = 50,
        CancellationToken ct   = default)
    {
        if (offset < 0)         return BadRequest(new { error = "offset_negative" });
        if (limit is < 1 or > 100)
            return BadRequest(new { error = "limit_out_of_range", min = 1, max = 100 });

        var page = await playlists.GetTracksAsync(User.RequireSessionId(), id, offset, limit, ct);
        return Ok(PlaylistContracts.MapTrackPage(page));
    }

    [HttpDelete("{id}/tracks")]
    public async Task<IActionResult> RemoveTracks(
        string id,
        [FromBody] RemoveTracksRequest request,
        CancellationToken ct)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest(new { error = "ids_required" });

        await playlists.RemoveTracksAsync(User.RequireSessionId(), id, request.Ids, ct);
        return NoContent();
    }
}
