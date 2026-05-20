using SpotifyDel.Domain.Sessions;

namespace SpotifyDel.Application.Abstractions;

public interface ISessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UserSession?> GetBySpotifyUserIdAsync(string spotifyUserId, CancellationToken ct);
    Task AddAsync(UserSession session, CancellationToken ct);
    Task UpdateAsync(UserSession session, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
