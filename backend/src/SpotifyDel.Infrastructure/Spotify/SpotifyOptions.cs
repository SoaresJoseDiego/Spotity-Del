namespace SpotifyDel.Infrastructure.Spotify;

public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
    public string ApiBaseUrl { get; init; } = "https://api.spotify.com/v1/";
    public string AccountsBaseUrl { get; init; } = "https://accounts.spotify.com/";
}
