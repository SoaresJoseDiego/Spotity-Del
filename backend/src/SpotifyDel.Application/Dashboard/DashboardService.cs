using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Application.Dashboard;

public sealed record GenreSlice(string Genre, int Count);

public sealed record DashboardInsight(string Label, string Value, string? Hint);

public sealed record DashboardOverview(
    int LikedTotal,
    int PlaylistTotal,
    string TimeRange,
    IReadOnlyList<TopArtist> TopArtists,
    IReadOnlyList<TopTrack> TopTracks,
    IReadOnlyList<GenreSlice> Genres,
    IReadOnlyList<DashboardInsight> Insights,
    IReadOnlyList<RecentPlay> RecentPlays);

public sealed class DashboardService(
    ISpotifyClient spotify,
    IAccessTokenAccessor tokens)
{
    public async Task<DashboardOverview> BuildAsync(
        Guid sessionId,
        string timeRange,
        CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);

        // Run all independent calls in parallel.
        var topArtistsTask  = spotify.GetTopArtistsAsync(token, timeRange, 50, ct);
        var topTracksTask   = spotify.GetTopTracksAsync(token, timeRange, 10, ct);
        var likedPageTask   = spotify.GetLikedTracksAsync(token, 0, 1, ct);
        var playlistsTask   = spotify.GetUserPlaylistsAsync(token, 0, 1, ct);
        var recentPlaysTask = SafeRecentPlaysAsync(spotify, token, ct);

        await Task.WhenAll(topArtistsTask, topTracksTask, likedPageTask, playlistsTask, recentPlaysTask);

        var topArtists  = await topArtistsTask;
        var topTracks   = await topTracksTask;
        var likedPage   = await likedPageTask;
        var playlistPage = await playlistsTask;
        var recentPlays = await recentPlaysTask;

        var genres = topArtists
            .SelectMany(a => a.Genres)
            .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GenreSlice(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToList();

        var insights = BuildInsights(topArtists, topTracks, genres, likedPage.Total, playlistPage.Total);

        return new DashboardOverview(
            LikedTotal:    likedPage.Total,
            PlaylistTotal: playlistPage.Total,
            TimeRange:     timeRange,
            TopArtists:    topArtists.Take(10).ToList(),
            TopTracks:     topTracks,
            Genres:        genres,
            Insights:      insights,
            RecentPlays:   recentPlays.Take(15).ToList());
    }

    // Recently-played is optional and shouldn't break the dashboard if it fails
    // (e.g. user just authenticated and there's no data, or transient API hiccup).
    private static async Task<IReadOnlyList<RecentPlay>> SafeRecentPlaysAsync(
        ISpotifyClient spotify, string token, CancellationToken ct)
    {
        try { return await spotify.GetRecentlyPlayedAsync(token, ct); }
        catch { return []; }
    }

    private static IReadOnlyList<DashboardInsight> BuildInsights(
        IReadOnlyList<TopArtist> topArtists,
        IReadOnlyList<TopTrack> topTracks,
        IReadOnlyList<GenreSlice> genres,
        int likedTotal,
        int playlistTotal)
    {
        var insights = new List<DashboardInsight>();

        if (topArtists.Count > 0)
        {
            var top = topArtists[0];
            var topGenres = top.Genres.Take(2).ToList();
            insights.Add(new DashboardInsight(
                "Artista nº1",
                top.Name,
                topGenres.Count > 0 ? string.Join(", ", topGenres) : null));
        }

        if (genres.Count > 0)
        {
            var topGenre = genres[0];
            var totalGenreHits = genres.Sum(g => g.Count);
            var pct = (int)Math.Round(100.0 * topGenre.Count / Math.Max(1, totalGenreHits));
            insights.Add(new DashboardInsight(
                "Gênero dominante",
                Capitalize(topGenre.Genre),
                $"{pct}% dos artistas do top"));
        }

        if (topTracks.Count > 0)
        {
            var longest = topTracks.OrderByDescending(t => t.DurationMs).First();
            insights.Add(new DashboardInsight(
                "Faixa mais longa do top",
                longest.Name,
                FormatDuration(longest.DurationMs)));

            var totalMs = topTracks.Sum(t => (long)t.DurationMs);
            var avgMs = (int)(totalMs / topTracks.Count);
            insights.Add(new DashboardInsight(
                "Duração média do top",
                FormatDuration(avgMs),
                $"{topTracks.Count} faixas"));
        }

        insights.Add(new DashboardInsight(
            "Biblioteca",
            $"{likedTotal:N0} curtidas",
            $"em {playlistTotal} playlist{(playlistTotal == 1 ? "" : "s")}"));

        return insights;
    }

    private static string FormatDuration(int ms)
    {
        var total = ms / 1000;
        return $"{total / 60}:{(total % 60).ToString("D2")}";
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
