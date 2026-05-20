using Microsoft.EntityFrameworkCore;
using SpotifyDel.Application.Abstractions;
using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Infrastructure.Persistence;

public sealed class SessionRepository(AppDbContext db) : ISessionRepository
{
    public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.UserSessions.Include(s => s.Tokens).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<UserSession?> GetBySpotifyUserIdAsync(string spotifyUserId, CancellationToken ct) =>
        db.UserSessions.Include(s => s.Tokens)
            .FirstOrDefaultAsync(s => s.SpotifyUserId == spotifyUserId, ct);

    public async Task AddAsync(UserSession session, CancellationToken ct)
    {
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken ct)
    {
        db.UserSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var session = await db.UserSessions.FindAsync([id], ct);
        if (session is null) return;
        db.UserSessions.Remove(session);
        await db.SaveChangesAsync(ct);
    }
}
