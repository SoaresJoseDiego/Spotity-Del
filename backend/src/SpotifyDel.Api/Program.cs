using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using SpotifyDel.Api;
using SpotifyDel.Api.Auth;
using SpotifyDel.Api.ErrorHandling;
using SpotifyDel.Application;
using SpotifyDel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<SpotifyExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Cross-origin cookies (frontend on a different domain than the API) require
// SameSite=None + Secure. In local dev we keep Lax so http works.
var crossOriginCookies = builder.Environment.IsProduction();

builder.Services
    .AddAuthentication(SessionClaims.Scheme)
    .AddCookie(SessionClaims.Scheme, opt =>
    {
        opt.Cookie.Name         = "spotifydel.session";
        opt.Cookie.HttpOnly     = true;
        opt.Cookie.SameSite     = crossOriginCookies ? SameSiteMode.None : SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = crossOriginCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        opt.ExpireTimeSpan      = TimeSpan.FromDays(30);
        opt.SlidingExpiration   = true;

        opt.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        opt.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

const string CorsPolicy = "spotifydel-frontend";
builder.Services.AddCors(opt => opt.AddPolicy(CorsPolicy, policy =>
{
    var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
    var origins = frontendUrl
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    policy.WithOrigins(origins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
}));

// Render/Heroku/etc terminate TLS at the edge and forward HTTP. Honor X-Forwarded-Proto
// so Request.IsHttps is true in prod (cookies + ASP.NET need this).
builder.Services.Configure<ForwardedHeadersOptions>(opt =>
{
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opt.KnownNetworks.Clear();
    opt.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.MigrateDatabaseAsync();

app.UseExceptionHandler();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
