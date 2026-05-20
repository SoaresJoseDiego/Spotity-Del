using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SpotifyUserId).IsRequired().HasMaxLength(64);
        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.AvatarUrl).HasMaxLength(1024);
        builder.HasIndex(s => s.SpotifyUserId).IsUnique();

        builder.HasOne(s => s.Tokens)
            .WithOne()
            .HasForeignKey<SpotifyTokens>(t => t.UserSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
