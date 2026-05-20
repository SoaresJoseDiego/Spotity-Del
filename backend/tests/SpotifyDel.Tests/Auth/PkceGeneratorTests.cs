using System.Security.Cryptography;
using System.Text;
using SpotifyDel.Application.Auth;

namespace SpotifyDel.Tests.Auth;

public class PkceGeneratorTests
{
    [Fact]
    public void Create_produces_distinct_values_per_call()
    {
        var (v1, c1) = PkceGenerator.Create();
        var (v2, c2) = PkceGenerator.Create();

        Assert.NotEqual(v1, v2);
        Assert.NotEqual(c1, c2);
    }

    [Fact]
    public void Challenge_is_base64url_sha256_of_verifier()
    {
        var (verifier, challenge) = PkceGenerator.Create();

        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        Assert.Equal(expected, challenge);
    }

    [Fact]
    public void Challenge_contains_only_url_safe_characters()
    {
        var (_, challenge) = PkceGenerator.Create();
        Assert.DoesNotContain('+', challenge);
        Assert.DoesNotContain('/', challenge);
        Assert.DoesNotContain('=', challenge);
    }

    [Fact]
    public void Verifier_length_is_within_rfc7636_bounds()
    {
        var (verifier, _) = PkceGenerator.Create();
        // RFC 7636 §4.1: 43..128 chars
        Assert.InRange(verifier.Length, 43, 128);
    }
}
