using Microsoft.Extensions.DependencyInjection;
using SpotifyDel.Application.Auth;
using SpotifyDel.Application.Dashboard;
using SpotifyDel.Application.Playlists;
using SpotifyDel.Application.Tracks;
using SpotifyDel.Application.Triage;

namespace SpotifyDel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<TracksService>();
        services.AddScoped<FilterService>();
        services.AddScoped<PlaylistsService>();
        services.AddScoped<TriageService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
