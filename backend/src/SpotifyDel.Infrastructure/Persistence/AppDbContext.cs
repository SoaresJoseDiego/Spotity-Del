using Microsoft.EntityFrameworkCore;
using SpotifyDel.Domain.History;
using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SpotifyTokens> SpotifyTokens => Set<SpotifyTokens>();
    public DbSet<RecentlyPlayedEntry> RecentlyPlayed => Set<RecentlyPlayedEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
