using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyDel.Api.Auth;
using SpotifyDel.Application.Dashboard;

namespace SpotifyDel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    private static readonly HashSet<string> ValidTimeRanges =
        new(StringComparer.OrdinalIgnoreCase) { "short_term", "medium_term", "long_term" };

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(
        [FromQuery] string timeRange = "medium_term",
        CancellationToken ct = default)
    {
        if (!ValidTimeRanges.Contains(timeRange))
            return BadRequest(new { error = "invalid_time_range", valid = ValidTimeRanges });

        var data = await dashboard.BuildAsync(User.RequireSessionId(), timeRange, ct);
        return Ok(new
        {
            likedTotal    = data.LikedTotal,
            playlistTotal = data.PlaylistTotal,
            timeRange     = data.TimeRange,
            topArtists = data.TopArtists.Select(a => new
            {
                id = a.Id, name = a.Name, imageUrl = a.ImageUrl,
                genres = a.Genres, popularity = a.Popularity, followers = a.Followers,
            }),
            topTracks = data.TopTracks.Select(t => new
            {
                id = t.Id, name = t.Name, durationMs = t.DurationMs,
                externalUrl = t.ExternalUrl, albumName = t.AlbumName, albumImageUrl = t.AlbumImageUrl,
                artists = t.Artists.Select(a => new { id = a.Id, name = a.Name }),
            }),
            genres = data.Genres.Select(g => new { genre = g.Genre, count = g.Count }),
            insights = data.Insights.Select(i => new { label = i.Label, value = i.Value, hint = i.Hint }),
        });
    }
}
