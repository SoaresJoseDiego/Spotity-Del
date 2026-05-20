using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Infrastructure.Auth;
using SpotifyDel.Infrastructure.BackgroundJobs;
using SpotifyDel.Infrastructure.Persistence;
using SpotifyDel.Infrastructure.Spotify;

namespace SpotifyDel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SpotifyOptions>()
            .Bind(configuration.GetSection(SpotifyOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId),     "Spotify:ClientId is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Spotify:ClientSecret is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RedirectUri),  "Spotify:RedirectUri is required.")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=spotifydel.db";

        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));

        services.AddDataProtection();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IAccessTokenAccessor, AccessTokenAccessor>();

        services.AddHostedService<RecentlyPlayedCollector>();

        services.AddHttpClient<ISpotifyAuthClient, SpotifyAuthClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SpotifyOptions>>().Value;
            http.BaseAddress = new Uri(o.AccountsBaseUrl);
            http.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<ISpotifyClient, SpotifyClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SpotifyOptions>>().Value;
            http.BaseAddress = new Uri(o.ApiBaseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("spotify-api", builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 4,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(500),
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException ||
                    args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests }      ||
                    args.Outcome.Result is { StatusCode: HttpStatusCode.ServiceUnavailable }   ||
                    args.Outcome.Result is { StatusCode: HttpStatusCode.BadGateway }),
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } delta)
                        return ValueTask.FromResult<TimeSpan?>(delta);
                    return ValueTask.FromResult<TimeSpan?>(null);
                },
            });
        });

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
