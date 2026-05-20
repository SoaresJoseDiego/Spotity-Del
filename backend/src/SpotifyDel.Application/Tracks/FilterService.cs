using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Music;

namespace SpotifyDel.Application.Tracks;

public sealed record FilterRequest(
    DateTimeOffset? AddedBefore,
    int? MinArtistOccurrences,
    IReadOnlyCollection<string>? ExcludeGenres);

public sealed record FilterMatch(LikedTrack Track, IReadOnlyList<string> Reasons);

/// Streams the user's liked library once, applies all enabled filters in-memory
/// and returns matches. Cheap because deletion is the expensive Spotify call.
public sealed class FilterService(
    ISpotifyClient spotify,
    IAccessTokenAccessor tokens)
{
    private const int PageSize = 50;

    public async Task<IReadOnlyList<FilterMatch>> EvaluateAsync(
        Guid sessionId,
        FilterRequest filter,
        CancellationToken ct)
    {
        var token = await tokens.GetValidAccessTokenAsync(sessionId, ct);
        var all = await LoadAllLikedAsync(token, ct);

        var artistCounts = CountArtistOccurrences(all);
        var artistGenres = filter.ExcludeGenres is { Count: > 0 }
            ? await spotify.GetArtistGenresAsync(token, ArtistIds(all), ct)
            : null;

        var matches = new List<FilterMatch>();
        foreach (var liked in all)
        {
            var reasons = new List<string>();

            if (filter.AddedBefore is { } cutoff && liked.AddedAt < cutoff)
                reasons.Add($"adicionada antes de {cutoff:yyyy-MM-dd}");

            if (filter.MinArtistOccurrences is { } min && min > 1)
            {
                var maxOcc = liked.Track.Artists.Max(a => artistCounts.GetValueOrDefault(a.Id, 0));
                if (maxOcc >= min)
                    reasons.Add($"artista aparece {maxOcc}x na biblioteca");
            }

            if (filter.ExcludeGenres is { Count: > 0 } excluded && artistGenres is not null)
            {
                var hits = liked.Track.Artists
                    .SelectMany(a => artistGenres.GetValueOrDefault(a.Id, []))
                    .Where(g => excluded.Contains(g, StringComparer.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();
                if (hits.Count > 0)
                    reasons.Add($"gênero excluído: {string.Join(", ", hits)}");
            }

            if (reasons.Count > 0)
                matches.Add(new FilterMatch(liked, reasons));
        }

        return matches;
    }

    private async Task<List<LikedTrack>> LoadAllLikedAsync(string token, CancellationToken ct)
    {
        var all = new List<LikedTrack>();
        var offset = 0;
        while (true)
        {
            var page = await spotify.GetLikedTracksAsync(token, offset, PageSize, ct);
            all.AddRange(page.Items);
            if (!page.HasMore) break;
            offset += page.Items.Count;
        }
        return all;
    }

    private static Dictionary<string, int> CountArtistOccurrences(IEnumerable<LikedTrack> tracks)
    {
        var counts = new Dictionary<string, int>();
        foreach (var t in tracks)
            foreach (var a in t.Track.Artists)
                counts[a.Id] = counts.GetValueOrDefault(a.Id, 0) + 1;
        return counts;
    }

    private static HashSet<string> ArtistIds(IEnumerable<LikedTrack> tracks) =>
        tracks.SelectMany(t => t.Track.Artists.Select(a => a.Id)).ToHashSet();
}
