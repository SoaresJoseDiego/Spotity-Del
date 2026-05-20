using System.Security.Claims;

namespace SpotifyDel.Api.Auth;

public static class SessionClaims
{
    public const string Scheme = "SpotifyDel";
    public const string SessionIdClaim = "spotifydel:session_id";

    public static Guid? GetSessionId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(SessionIdClaim);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static Guid RequireSessionId(this ClaimsPrincipal user) =>
        user.GetSessionId()
            ?? throw new UnauthorizedAccessException("Authenticated principal has no session id.");
}
