using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyDel.Api.Auth;
using SpotifyDel.Api.Contracts;
using SpotifyDel.Application.Tracks;

namespace SpotifyDel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tracks")]
public sealed class TracksController(
    TracksService tracks,
    FilterService filters) : ControllerBase
{
    [HttpGet("liked")]
    public async Task<IActionResult> Liked(
        [FromQuery] int offset = 0,
        [FromQuery] int limit  = 50,
        CancellationToken ct   = default)
    {
        if (offset < 0)         return BadRequest(new { error = "offset_negative" });
        if (limit is < 1 or > 50)
            return BadRequest(new { error = "limit_out_of_range", min = 1, max = 50 });

        var page = await tracks.GetLikedAsync(User.RequireSessionId(), offset, limit, ct);
        return Ok(LikedTrackContracts.MapPage(page));
    }

    [HttpDelete("liked")]
    public async Task<IActionResult> Remove(
        [FromBody] RemoveTracksRequest request,
        CancellationToken ct)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest(new { error = "ids_required" });

        await tracks.RemoveAsync(User.RequireSessionId(), request.Ids, ct);
        return NoContent();
    }

    [HttpPost("filter")]
    public async Task<IActionResult> Filter(
        [FromBody] FilterRequestDto request,
        CancellationToken ct)
    {
        var domain = new FilterRequest(
            request.AddedBefore,
            request.MinArtistOccurrences,
            request.ExcludeGenres);

        var matches = await filters.EvaluateAsync(User.RequireSessionId(), domain, ct);
        return Ok(matches.Select(m => new
        {
            track   = LikedTrackContracts.Map(m.Track),
            reasons = m.Reasons,
        }));
    }
}
