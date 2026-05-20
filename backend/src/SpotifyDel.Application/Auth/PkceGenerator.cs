using System.Security.Cryptography;
using System.Text;

namespace SpotifyDel.Application.Auth;

public static class PkceGenerator
{
    public static (string Verifier, string Challenge) Create()
    {
        Span<byte> raw = stackalloc byte[64];
        RandomNumberGenerator.Fill(raw);
        var verifier = Base64Url(raw);

        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64Url(challengeBytes);

        return (verifier, challenge);
    }

    public static string RandomState(int byteLength = 32)
    {
        Span<byte> raw = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(raw);
        return Base64Url(raw);
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
