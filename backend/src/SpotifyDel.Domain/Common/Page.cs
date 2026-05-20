namespace SpotifyDel.Domain.Common;

public sealed record Page<T>(
    IReadOnlyList<T> Items,
    int Offset,
    int Limit,
    int Total)
{
    public bool HasMore => Offset + Items.Count < Total;
}
