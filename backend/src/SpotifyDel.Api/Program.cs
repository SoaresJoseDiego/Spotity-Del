using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services
    .AddAuthentication(SessionClaims.Scheme)
    .AddCookie(SessionClaims.Scheme, opt =>
    {
        opt.Cookie.Name        = "spotifydel.session";
        opt.Cookie.HttpOnly    = true;
        opt.Cookie.SameSite    = SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opt.ExpireTimeSpan     = TimeSpan.FromDays(30);
        opt.SlidingExpiration  = true;

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
    policy.WithOrigins(frontendUrl)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
}));

var app = builder.Build();

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

app.Run();
