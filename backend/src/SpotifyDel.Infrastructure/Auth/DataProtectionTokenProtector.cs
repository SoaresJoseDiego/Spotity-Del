using Microsoft.AspNetCore.DataProtection;
using SpotifyDel.Application.Abstractions;

namespace SpotifyDel.Infrastructure.Auth;

public sealed class DataProtectionTokenProtector(IDataProtectionProvider provider) : ITokenProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("SpotifyDel.Tokens.v1");

    public string Protect(string plaintext) => protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => protector.Unprotect(ciphertext);
}
