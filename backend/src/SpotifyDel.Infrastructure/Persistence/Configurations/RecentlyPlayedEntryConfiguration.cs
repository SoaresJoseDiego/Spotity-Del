using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyDel.Domain.History;

namespace SpotifyDel.Infrastructure.Persistence.Configurations;

public sealed class RecentlyPlayedEntryConfiguration : IEntityTypeConfiguration<RecentlyPlayedEntry>
{
    public void Configure(EntityTypeBuilder<RecentlyPlayedEntry> builder)
    {
        builder.ToTable("recently_played");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TrackId).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.UserSessionId, e.TrackId, e.PlayedAt }).IsUnique();
    }
}
