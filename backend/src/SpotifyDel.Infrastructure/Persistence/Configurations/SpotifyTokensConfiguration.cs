using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Infrastructure.Persistence.Configurations;

public sealed class SpotifyTokensConfiguration : IEntityTypeConfiguration<SpotifyTokens>
{
    public void Configure(EntityTypeBuilder<SpotifyTokens> builder)
    {
        builder.ToTable("spotify_tokens");
        builder.HasKey(t => t.UserSessionId);
        builder.Property(t => t.AccessTokenEncrypted).IsRequired();
        builder.Property(t => t.RefreshTokenEncrypted).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.Scopes).IsRequired().HasMaxLength(512).HasDefaultValue(string.Empty);
    }
}
