using System.Text.Json;

namespace SpotifyDel.Infrastructure.Spotify;

internal static class SpotifyResponseExtensions
{
    /// Throws SpotifyApiException with Spotify's `{ "error": { "message": "..." } }` payload
    /// when the response is not 2xx. Replaces HttpClient.EnsureSuccessStatusCode so the
    /// underlying Spotify diagnostic message survives to logs and callers.
    public static async Task EnsureSpotifySuccessAsync(
        this HttpResponseMessage response,
        string endpoint,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var apiMessage = TryExtractMessage(body) ?? (string.IsNullOrWhiteSpace(body) ? (response.ReasonPhrase ?? "(no body)") : body);

        var headers = new List<string>();
        foreach (var h in response.Headers)
            headers.Add($"{h.Key}: {string.Join(',', h.Value)}");
        foreach (var h in response.Content.Headers)
            headers.Add($"{h.Key}: {string.Join(',', h.Value)}");
        var headerDump = headers.Count > 0 ? string.Join(" | ", headers) : "(no headers)";

        throw new SpotifyApiException(
            response.StatusCode,
            $"{apiMessage}  ::  body={body}  ::  headers=[{headerDump}]",
            endpoint);
    }

    private static string? TryExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString();
            }
        }
        catch (JsonException) { }
        return null;
    }
}
