using System.Net;

namespace SpotifyDel.Application.Auth;

public static class SpotifyAuthorizationUrlBuilder
{
    private const string Endpoint = "https://accounts.spotify.com/authorize";

    public static string Build(
        string clientId,
        string redirectUri,
        string state,
        string codeChallenge,
        IEnumerable<string> scopes)
    {
        var query = string.Join('&', new[]
        {
            $"client_id={WebUtility.UrlEncode(clientId)}",
            "response_type=code",
            $"redirect_uri={WebUtility.UrlEncode(redirectUri)}",
            $"state={WebUtility.UrlEncode(state)}",
            "code_challenge_method=S256",
            $"code_challenge={WebUtility.UrlEncode(codeChallenge)}",
            $"scope={WebUtility.UrlEncode(string.Join(' ', scopes))}",
            "show_dialog=true",
        });
        return $"{Endpoint}?{query}";
    }

    public static IReadOnlyList<string> DefaultScopes { get; } =
    [
        "user-read-private",
        "user-read-email",
        "user-library-read",
        "user-library-modify",
        "user-read-recently-played",
        "user-top-read",
        "playlist-read-private",
    ];
}
