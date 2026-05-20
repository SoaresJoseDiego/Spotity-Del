namespace SpotifyDel.Domain.Music;

public sealed record Track(
    string Id,
    string Name,
    int DurationMs,
    string? PreviewUrl,
    string ExternalUrl,
    Album Album,
    IReadOnlyList<Artist> Artists);
