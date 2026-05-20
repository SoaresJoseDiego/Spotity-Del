using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.History;
using SpotifyDel.Infrastructure.Persistence;

namespace SpotifyDel.Infrastructure.BackgroundJobs;

/// Hits Spotify's /me/player/recently-played for each active session every interval
/// and persists new plays. The endpoint returns only the last 50 plays, so the value
/// of this collector compounds over time — it's how we build long-term history that
/// the API itself doesn't expose.
public sealed class RecentlyPlayedCollector(
    IServiceProvider services,
    ILogger<RecentlyPlayedCollector> logger,
    TimeProvider clock) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recently-played collector cycle failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task CollectOnceAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var spotify = scope.ServiceProvider.GetRequiredService<ISpotifyClient>();
        var tokenAccessor = scope.ServiceProvider.GetRequiredService<IAccessTokenAccessor>();

        var sessionIds = await db.UserSessions.AsNoTracking().Select(s => s.Id).ToListAsync(ct);
        if (sessionIds.Count == 0) return;

        var totalInserted = 0;
        foreach (var sessionId in sessionIds)
        {
            try
            {
                var token = await tokenAccessor.GetValidAccessTokenAsync(sessionId, ct);
                var plays = await spotify.GetRecentlyPlayedAsync(token, ct);
                if (plays.Count == 0) continue;

                var keys = plays.Select(p => (p.TrackId, p.PlayedAt)).ToHashSet();
                var existing = await db.RecentlyPlayed
                    .Where(e => e.UserSessionId == sessionId
                                && plays.Select(p => p.TrackId).Contains(e.TrackId))
                    .Select(e => new { e.TrackId, e.PlayedAt })
                    .ToListAsync(ct);

                var existingSet = existing.Select(e => (e.TrackId, e.PlayedAt)).ToHashSet();
                var newOnes = plays
                    .Where(p => !existingSet.Contains((p.TrackId, p.PlayedAt)))
                    .Select(p => new RecentlyPlayedEntry
                    {
                        Id = Guid.NewGuid(),
                        UserSessionId = sessionId,
                        TrackId = p.TrackId,
                        PlayedAt = p.PlayedAt,
                    })
                    .ToList();

                if (newOnes.Count == 0) continue;

                db.RecentlyPlayed.AddRange(newOnes);
                await db.SaveChangesAsync(ct);
                totalInserted += newOnes.Count;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect recently-played for session {SessionId}", sessionId);
            }
        }

        if (totalInserted > 0)
            logger.LogInformation("Collected {Count} new plays at {At}", totalInserted, clock.GetUtcNow());
    }
}
