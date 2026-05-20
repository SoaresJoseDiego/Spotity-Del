namespace SpotifyDel.Application.Abstractions;

public interface ITokenProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
