using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SpotifyDel.Api.Auth;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Application.Auth;
using SpotifyDel.Infrastructure.Spotify;

namespace SpotifyDel.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AuthService auth,
    ISessionRepository sessions,
    IOptions<SpotifyOptions> spotifyOptions,
    IOptions<FrontendOptions> frontendOptions) : ControllerBase
{
    private const string PkceCookie  = "spotifydel.pkce";
    private const string StateCookie = "spotifydel.state";

    private readonly SpotifyOptions  spotify  = spotifyOptions.Value;
    private readonly FrontendOptions frontend = frontendOptions.Value;

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var (verifier, challenge) = PkceGenerator.Create();
        var state = PkceGenerator.RandomState();

        var cookieOptions = ShortLivedCookie();
        Response.Cookies.Append(PkceCookie,  verifier, cookieOptions);
        Response.Cookies.Append(StateCookie, state,    cookieOptions);

        var url = SpotifyAuthorizationUrlBuilder.Build(
            spotify.ClientId,
            spotify.RedirectUri,
            state,
            challenge,
            SpotifyAuthorizationUrlBuilder.DefaultScopes);

        return Redirect(url);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontend.BaseUrl}/login?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest(new { error = "missing_code_or_state" });

        if (!Request.Cookies.TryGetValue(StateCookie, out var savedState))
            return BadRequest(new
            {
                error = "state_cookie_missing",
                hint  = "Probably navigated via 'localhost' but callback hits '127.0.0.1'. Use http://127.0.0.1:4200 in the browser.",
                cookiesReceived = Request.Cookies.Keys.ToArray(),
            });

        if (savedState != state)
            return BadRequest(new { error = "state_value_mismatch" });

        if (!Request.Cookies.TryGetValue(PkceCookie, out var verifier) || string.IsNullOrEmpty(verifier))
            return BadRequest(new { error = "pkce_cookie_missing" });

        Response.Cookies.Delete(StateCookie);
        Response.Cookies.Delete(PkceCookie);

        var session = await auth.CompleteLoginAsync(code, verifier, spotify.RedirectUri, ct);

        var claims = new ClaimsIdentity(
            [
                new Claim(SessionClaims.SessionIdClaim, session.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, session.SpotifyUserId),
                new Claim(ClaimTypes.Name, session.DisplayName),
            ],
            SessionClaims.Scheme);

        await HttpContext.SignInAsync(SessionClaims.Scheme, new ClaimsPrincipal(claims));

        return Redirect(frontend.BaseUrl);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sessionId = User.RequireSessionId();
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session is null) return Unauthorized();

        return Ok(new
        {
            id = session.SpotifyUserId,
            displayName = session.DisplayName,
            avatarUrl = session.AvatarUrl,
            scopes = session.Tokens?.Scopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [],
            tokenExpiresAt = session.Tokens?.ExpiresAt,
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sessionId = User.RequireSessionId();
        await auth.LogoutAsync(sessionId, ct);
        await HttpContext.SignOutAsync(SessionClaims.Scheme);
        return NoContent();
    }

    private CookieOptions ShortLivedCookie() => new()
    {
        HttpOnly = true,
        Secure   = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires  = DateTimeOffset.UtcNow.AddMinutes(10),
        Path     = "/api/auth",
    };
}
