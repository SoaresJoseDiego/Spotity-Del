namespace SpotifyDel.Application.Abstractions;

public interface IAccessTokenAccessor
{
    Task<string> GetValidAccessTokenAsync(Guid sessionId, CancellationToken ct);
}
