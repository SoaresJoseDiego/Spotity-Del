using System.Net.Http.Headers;
using System.Net.Http.Json;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Common;
using SpotifyDel.Domain.Music;
using SpotifyDel.Infrastructure.Spotify.Models;

namespace SpotifyDel.Infrastructure.Spotify;

public sealed class SpotifyClient(HttpClient http) : ISpotifyClient
{
    public const string HttpClientName = "spotify-api";

    public async Task<SpotifyUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "me");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyUserDto>(ct)
            ?? throw new InvalidOperationException("Empty /me response.");

        return new SpotifyUserProfile(
            dto.Id,
            dto.DisplayName ?? dto.Id,
            dto.Images?.FirstOrDefault()?.Url,
            dto.Email);
    }

    public async Task<Page<LikedTrack>> GetLikedTracksAsync(
        string accessToken,
        int offset,
        int limit,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"me/tracks?offset={offset}&limit={limit}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyPageDto<SpotifySavedTrackDto>>(ct)
            ?? throw new InvalidOperationException("Empty /me/tracks response.");

        var items = dto.Items
            .Where(i => i.Track is not null && !string.IsNullOrEmpty(i.Track.Id))
            .Select(i => new LikedTrack(MapTrack(i.Track!), i.AddedAt))
            .ToList();
        return new Page<LikedTrack>(items, dto.Offset, dto.Limit, dto.Total);
    }

    public async Task RemoveLikedTracksAsync(
        string accessToken,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct)
    {
        if (trackIds.Count == 0) return;
        // Feb/2026 migration: /me/tracks moved to /me/library. Max batch dropped from 50 to 40.
        // Despite the official migration guide saying "body with uris array", the endpoint
        // actually requires uris as a comma-separated query parameter (verified empirically).
        if (trackIds.Count > 40)
            throw new ArgumentException("Spotify accepts at most 40 URIs per request.", nameof(trackIds));

        var uris = string.Join(',', trackIds.Select(id => $"spotify:track:{id}"));
        using var req = new HttpRequestMessage(
            HttpMethod.Delete,
            $"me/library?uris={Uri.EscapeDataString(uris)}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);
    }

    public async Task<Page<Playlist>> GetUserPlaylistsAsync(
        string accessToken,
        int offset,
        int limit,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"me/playlists?offset={offset}&limit={limit}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyPageDto<SpotifyPlaylistDto>>(ct)
            ?? throw new InvalidOperationException("Empty /me/playlists response.");

        var items = dto.Items
            .Where(p => p is not null && !string.IsNullOrEmpty(p.Id))
            .Select(p => new Playlist(
                p.Id!,
                p.Name ?? "(sem nome)",
                string.IsNullOrWhiteSpace(p.Description) ? null : p.Description,
                p.Images?.FirstOrDefault()?.Url,
                p.Owner?.Id ?? "unknown",
                p.Owner?.DisplayName ?? p.Owner?.Id ?? "unknown",
                p.Tracks?.Total ?? 0,
                p.Collaborative,
                p.Public ?? false))
            .ToList();

        return new Page<Playlist>(items, dto.Offset, dto.Limit, dto.Total);
    }

    public async Task<Page<PlaylistTrack>> GetPlaylistTracksAsync(
        string accessToken,
        string playlistId,
        int offset,
        int limit,
        CancellationToken ct)
    {
        // Spotify deprecated /tracks; new endpoint is /items (works for tracks + episodes).
        // Old /tracks returns 403 for dev-mode apps as part of the migration push.
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"playlists/{Uri.EscapeDataString(playlistId)}/items?offset={offset}&limit={limit}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyPageDto<SpotifyPlaylistItemDto>>(ct)
            ?? throw new InvalidOperationException("Empty playlist tracks response.");

        var items = dto.Items
            .Where(i => i.Item is not null
                     && !i.IsLocal
                     && !string.IsNullOrEmpty(i.Item.Id)
                     && i.Item.Type != "episode")
            .Select(i => new PlaylistTrack(MapTrack(i.Item!), i.AddedAt ?? DateTimeOffset.MinValue))
            .ToList();

        return new Page<PlaylistTrack>(items, dto.Offset, dto.Limit, dto.Total);
    }

    public async Task RemovePlaylistTracksAsync(
        string accessToken,
        string playlistId,
        IReadOnlyCollection<string> trackIds,
        CancellationToken ct)
    {
        if (trackIds.Count == 0) return;
        if (trackIds.Count > 100)
            throw new ArgumentException("Spotify accepts at most 100 tracks per playlist-removal request.", nameof(trackIds));

        // Feb/2026 migration: body key "tracks" renamed to "items".
        var body = new
        {
            items = trackIds.Select(id => new { uri = $"spotify:track:{id}" }).ToArray(),
        };

        using var req = new HttpRequestMessage(
            HttpMethod.Delete,
            $"playlists/{Uri.EscapeDataString(playlistId)}/items")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);
    }

    public async Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
        string accessToken,
        string timeRange,
        int limit,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"me/top/artists?time_range={Uri.EscapeDataString(timeRange)}&limit={limit}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyPageDto<SpotifyArtistDto>>(ct);
        if (dto?.Items is null) return [];

        return dto.Items
            .Where(a => !string.IsNullOrEmpty(a.Id))
            .Select(a => new TopArtist(
                a.Id, a.Name,
                a.Images?.FirstOrDefault()?.Url,
                a.Genres ?? [],
                a.Popularity ?? 0,
                a.Followers?.Total ?? 0))
            .ToList();
    }

    public async Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
        string accessToken,
        string timeRange,
        int limit,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"me/top/tracks?time_range={Uri.EscapeDataString(timeRange)}&limit={limit}");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyPageDto<SpotifyTrackDto>>(ct);
        if (dto?.Items is null) return [];

        return dto.Items
            .Where(t => !string.IsNullOrEmpty(t.Id))
            .Select(t => new TopTrack(
                t.Id!, t.Name ?? "(sem nome)", t.DurationMs,
                t.ExternalUrls?.GetValueOrDefault("spotify") ?? $"https://open.spotify.com/track/{t.Id}",
                t.Album?.Name ?? "(sem álbum)",
                t.Album?.Images?.FirstOrDefault()?.Url,
                t.Artists?.Select(a => new Artist(a.Id, a.Name)).ToList() ?? [],
                0))
            .ToList();
    }

    public async Task<IReadOnlyList<RecentPlay>> GetRecentlyPlayedAsync(
        string accessToken,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "me/player/recently-played?limit=50");
        req.Headers.Authorization = Bearer(accessToken);

        using var response = await http.SendAsync(req, ct);
        await response.EnsureSpotifySuccessAsync(req.RequestUri?.ToString() ?? "spotify", ct);

        var dto = await response.Content.ReadFromJsonAsync<SpotifyRecentlyPlayedResponseDto>(ct);
        if (dto?.Items is null) return [];

        return dto.Items
            .Where(i => i.Track is not null && !string.IsNullOrEmpty(i.Track.Id))
            .Select(i => new RecentPlay(i.Track!.Id!, i.PlayedAt))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetArtistGenresAsync(
        string accessToken,
        IReadOnlyCollection<string> artistIds,
        CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();
        if (artistIds.Count == 0) return result;

        foreach (var batch in artistIds.Chunk(50))
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"artists?ids={string.Join(',', batch)}");
            req.Headers.Authorization = Bearer(accessToken);

            using var response = await http.SendAsync(req, ct);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<SpotifyArtistsResponseDto>(ct);
            if (dto is null) continue;

            foreach (var a in dto.Artists)
                result[a.Id] = a.Genres ?? [];
        }

        return result;
    }

    private static Track MapTrack(SpotifyTrackDto t) => new(
        t.Id ?? string.Empty,
        t.Name ?? "(sem nome)",
        t.DurationMs,
        t.PreviewUrl,
        t.ExternalUrls?.GetValueOrDefault("spotify") ?? $"https://open.spotify.com/track/{t.Id}",
        t.Album is null
            ? new Album(string.Empty, "(sem álbum)", null)
            : new Album(t.Album.Id, t.Album.Name, t.Album.Images?.FirstOrDefault()?.Url),
        t.Artists?.Select(a => new Artist(a.Id, a.Name)).ToList() ?? []);

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);
}
