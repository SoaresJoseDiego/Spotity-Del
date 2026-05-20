using System.Text.Json.Serialization;

namespace SpotifyDel.Infrastructure.Spotify.Models;

internal sealed record SpotifyTokenDto(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")]    int ExpiresIn,
    [property: JsonPropertyName("scope")]         string Scope,
    [property: JsonPropertyName("token_type")]    string TokenType);

internal sealed record SpotifyImageDto(
    [property: JsonPropertyName("url")]    string Url,
    [property: JsonPropertyName("width")]  int? Width,
    [property: JsonPropertyName("height")] int? Height);

internal sealed record SpotifyUserDto(
    [property: JsonPropertyName("id")]           string Id,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("email")]        string? Email,
    [property: JsonPropertyName("images")]       IReadOnlyList<SpotifyImageDto>? Images);

internal sealed record SpotifyArtistDto(
    [property: JsonPropertyName("id")]         string Id,
    [property: JsonPropertyName("name")]       string Name,
    [property: JsonPropertyName("genres")]     IReadOnlyList<string>? Genres,
    [property: JsonPropertyName("images")]     IReadOnlyList<SpotifyImageDto>? Images,
    [property: JsonPropertyName("popularity")] int? Popularity,
    [property: JsonPropertyName("followers")]  SpotifyFollowersDto? Followers);

internal sealed record SpotifyFollowersDto(
    [property: JsonPropertyName("total")] int Total);

internal sealed record SpotifyAlbumDto(
    [property: JsonPropertyName("id")]     string Id,
    [property: JsonPropertyName("name")]   string Name,
    [property: JsonPropertyName("images")] IReadOnlyList<SpotifyImageDto>? Images);

internal sealed record SpotifyTrackDto(
    [property: JsonPropertyName("id")]            string? Id,
    [property: JsonPropertyName("name")]          string? Name,
    [property: JsonPropertyName("duration_ms")]   int DurationMs,
    [property: JsonPropertyName("preview_url")]   string? PreviewUrl,
    [property: JsonPropertyName("external_urls")] Dictionary<string, string>? ExternalUrls,
    [property: JsonPropertyName("album")]         SpotifyAlbumDto? Album,
    [property: JsonPropertyName("artists")]       IReadOnlyList<SpotifyArtistDto>? Artists,
    [property: JsonPropertyName("type")]          string? Type);

internal sealed record SpotifySavedTrackDto(
    [property: JsonPropertyName("added_at")] DateTimeOffset AddedAt,
    [property: JsonPropertyName("track")]    SpotifyTrackDto Track);

internal sealed record SpotifyPageDto<T>(
    [property: JsonPropertyName("items")]  IReadOnlyList<T> Items,
    [property: JsonPropertyName("offset")] int Offset,
    [property: JsonPropertyName("limit")]  int Limit,
    [property: JsonPropertyName("total")]  int Total,
    [property: JsonPropertyName("next")]   string? Next);

internal sealed record SpotifyArtistsResponseDto(
    [property: JsonPropertyName("artists")] IReadOnlyList<SpotifyArtistDto> Artists);

internal sealed record SpotifyPlaylistOwnerDto(
    [property: JsonPropertyName("id")]           string Id,
    [property: JsonPropertyName("display_name")] string? DisplayName);

internal sealed record SpotifyPlaylistTracksRefDto(
    [property: JsonPropertyName("total")] int Total);

internal sealed record SpotifyPlaylistDto(
    [property: JsonPropertyName("id")]            string? Id,
    [property: JsonPropertyName("name")]          string? Name,
    [property: JsonPropertyName("description")]   string? Description,
    [property: JsonPropertyName("images")]        IReadOnlyList<SpotifyImageDto>? Images,
    [property: JsonPropertyName("owner")]         SpotifyPlaylistOwnerDto? Owner,
    [property: JsonPropertyName("tracks")]        SpotifyPlaylistTracksRefDto? Tracks,
    [property: JsonPropertyName("collaborative")] bool Collaborative,
    [property: JsonPropertyName("public")]        bool? Public);

internal sealed record SpotifyPlaylistItemDto(
    [property: JsonPropertyName("added_at")] DateTimeOffset? AddedAt,
    [property: JsonPropertyName("item")]     SpotifyTrackDto? Item,
    [property: JsonPropertyName("is_local")] bool IsLocal);

internal sealed record SpotifyRecentlyPlayedItemDto(
    [property: JsonPropertyName("track")]     SpotifyTrackDto? Track,
    [property: JsonPropertyName("played_at")] DateTimeOffset PlayedAt);

internal sealed record SpotifyRecentlyPlayedResponseDto(
    [property: JsonPropertyName("items")] IReadOnlyList<SpotifyRecentlyPlayedItemDto>? Items);
