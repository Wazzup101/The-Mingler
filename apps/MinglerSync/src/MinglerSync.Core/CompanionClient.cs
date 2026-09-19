using System.Security.Cryptography;
using System.Text.Json;

namespace MinglerSync.Core;

public sealed record CompanionProfile(int Port, string DisplayName, JsonElement Payload);

public sealed class CompanionClient
{
    public const int FirstPort = 1547;
    public const int LastPort = 1562;

    private const string ProductVersion = "1.0.2";
    private static readonly TimeSpan PortTimeout = TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<CompanionProfile>> FindProfilesAsync(CancellationToken cancellationToken = default)
    {
        var authorization = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var scans = Enumerable.Range(FirstPort, LastPort - FirstPort + 1)
            .Select(port => ReadPortAsync(port, authorization, cancellationToken));

        var results = await Task.WhenAll(scans);
        return results
            .Where(profile => profile is not null)
            .Cast<CompanionProfile>()
            .OrderBy(profile => profile.Port)
            .ToArray();
    }

    private static async Task<CompanionProfile?> ReadPortAsync(
        int port,
        string authorization,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PortTimeout);

        try
        {
            using var handler = new SocketsHttpHandler { UseProxy = false };
            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/all.json");
            request.Headers.Host = $"localhost:{port}";
            request.Headers.TryAddWithoutValidation(
                "User-Agent",
                $"The Mingler/{ProductVersion} (Discord Bot - Mingler Sync) Windows");
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: timeout.Token);
            var payload = document.RootElement.Clone();
            return new CompanionProfile(port, FindToonLabel(payload) ?? $"Toon on port {port}", payload);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? FindToonLabel(JsonElement payload)
    {
        if (payload.TryGetProperty("toon", out var toon) && TryGetString(toon, "name", out var toonName))
        {
            return toonName;
        }

        if (TryGetString(payload, "name", out var rootName))
        {
            return rootName;
        }

        if (payload.TryGetProperty("sessions", out var sessions) && sessions.ValueKind == JsonValueKind.Array)
        {
            var names = sessions.EnumerateArray()
                .Select(session => session.TryGetProperty("toon", out var sessionToon) &&
                                   TryGetString(sessionToon, "name", out var name) ? name : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            if (names.Length > 0)
            {
                return string.Join(", ", names!);
            }
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
