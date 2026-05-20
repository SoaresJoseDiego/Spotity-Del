using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Infrastructure.Spotify.Models;

namespace SpotifyDel.Infrastructure.Spotify;

public sealed class SpotifyAuthClient(
    HttpClient http,
    IOptions<SpotifyOptions> options) : ISpotifyAuthClient
{
    public const string HttpClientName = "spotify-accounts";

    private readonly SpotifyOptions opts = options.Value;

    public async Task<SpotifyTokenResponse> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
            ["client_id"]     = opts.ClientId,
            ["code_verifier"] = codeVerifier,
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/token") { Content = form };
        req.Headers.Authorization = BasicAuthHeader(opts.ClientId, opts.ClientSecret);

        return await SendAsync(req, ct);
    }

    public async Task<SpotifyTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"]     = opts.ClientId,
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/token") { Content = form };
        req.Headers.Authorization = BasicAuthHeader(opts.ClientId, opts.ClientSecret);

        return await SendAsync(req, ct);
    }

    private async Task<SpotifyTokenResponse> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var response = await http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<SpotifyTokenDto>(ct)
            ?? throw new InvalidOperationException("Empty token response from Spotify.");

        return new SpotifyTokenResponse(dto.AccessToken, dto.RefreshToken, dto.ExpiresIn, dto.Scope);
    }

    private static System.Net.Http.Headers.AuthenticationHeaderValue BasicAuthHeader(string id, string secret)
    {
        var raw = Encoding.UTF8.GetBytes($"{id}:{secret}");
        return new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));
    }
}
