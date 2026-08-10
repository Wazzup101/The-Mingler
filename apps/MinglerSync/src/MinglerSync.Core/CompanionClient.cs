using System.Security.Cryptography;
using System.Text.Json;

namespace MinglerSync.Core;

public sealed record CompanionProfile(int Port, string DisplayName, JsonElement Payload);

public sealed class CompanionClient
{
    public async Task<IReadOnlyList<CompanionProfile>> FindProfilesAsync(CancellationToken cancellationToken = default)
    {
        var authorization = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var scans = Enumerable.Range(1547, 8).Select(port => ReadPortAsync(port, authorization, cancellationToken));
        var results = await Task.WhenAll(scans);
        return results.Where(profile => profile is not null).Cast<CompanionProfile>().OrderBy(profile => profile.Port).ToArray();
    }

    private static async Task<CompanionProfile?> ReadPortAsync(int port, string authorization, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var handler = new SocketsHttpHandler { UseProxy = false };
            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/all.json");
            request.Headers.Host = $"localhost:{port}";
            request.Headers.TryAddWithoutValidation("User-Agent", "The Mingler/1.0.0 (Discord Bot - Mingler Sync) Windows");
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) return null;
            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: timeout.Token);
            var payload = document.RootElement.Clone();
            return new CompanionProfile(port, FindToonLabel(payload) ?? $"Toon on port {port}", payload);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            return null;
        }
    }

    internal static string? FindToonLabel(JsonElement payload)
    {
        if (payload.TryGetProperty("toon", out var toon) && TryName(toon, out var toonName)) return toonName;
        return TryName(payload, out var rootName) ? rootName : null;
    }

    private static bool TryName(JsonElement element, out string? value)
    {
        value = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("name", out var name) &&
                name.ValueKind == JsonValueKind.String ? name.GetString() : null;
        return !string.IsNullOrWhiteSpace(value);
    }
}

