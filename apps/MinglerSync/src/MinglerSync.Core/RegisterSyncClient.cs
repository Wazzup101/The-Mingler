using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MinglerSync.Core;

public sealed record ToonSyncResult(string Name, bool WasNew, IReadOnlyList<string> Updated);

public sealed record RegisterSyncResult(
    bool IsSuccess,
    string Message,
    int SyncedToons = 0,
    IReadOnlyList<ToonSyncResult>? Toons = null);

public sealed class RegisterSyncClient
{
    private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(60);

    public async Task<RegisterSyncResult> UploadAsync(
        PairingDetails pairing,
        IReadOnlyList<CompanionProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0)
        {
            return new RegisterSyncResult(false, "No toon data is ready to send.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(UploadTimeout);
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(HttpMethod.Post, pairing.SyncUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairing.Token);
        request.Content = JsonContent.Create(new { companions = profiles.Select(profile => profile.Payload).ToArray() });

        try
        {
            using var response = await client.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new RegisterSyncResult(false, GetFriendlyFailure(response.StatusCode));
            }

            return ParseSuccess(body, profiles.Count);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RegisterSyncResult(false, "The upload timed out. Check your internet connection, then run /register link again.");
        }
        catch (HttpRequestException)
        {
            return new RegisterSyncResult(false, "The app could not reach The Mingler. Check your connection or VPN, then run /register link again.");
        }
    }

    private static RegisterSyncResult ParseSuccess(string body, int fallbackCount)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var count = root.TryGetProperty("syncedToons", out var countNode) && countNode.TryGetInt32(out var parsedCount)
                ? parsedCount
                : fallbackCount;
            var message = root.TryGetProperty("message", out var messageNode)
                ? messageNode.GetString() ?? $"Registered {count} toon(s)."
                : $"Registered {count} toon(s).";

            var toons = new List<ToonSyncResult>();
            if (root.TryGetProperty("toons", out var toonArray) && toonArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var toon in toonArray.EnumerateArray())
                {
                    var name = toon.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? "Toon" : "Toon";
                    var wasNew = toon.TryGetProperty("wasNew", out var newNode) && newNode.ValueKind == JsonValueKind.True;
                    var updated = toon.TryGetProperty("updated", out var updatedNode) && updatedNode.ValueKind == JsonValueKind.Array
                        ? updatedNode.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null).Cast<string>().ToArray()
                        : Array.Empty<string>();
                    toons.Add(new ToonSyncResult(name, wasNew, updated));
                }
            }

            return new RegisterSyncResult(true, message, count, toons);
        }
        catch (JsonException)
        {
            return new RegisterSyncResult(true, $"Registered {fallbackCount} toon(s).", fallbackCount);
        }
    }

    private static string GetFriendlyFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "That pairing link expired or was already used. Run /register link again and paste the new command.",
        HttpStatusCode.Forbidden => "The server rejected the connection. Turn off your VPN and make sure you are using the network described in Discord.",
        HttpStatusCode.RequestEntityTooLarge => "The toon data was too large. Try again with fewer game accounts open, or contact The Mingler staff.",
        HttpStatusCode.UnprocessableEntity => "The server received the data but could not import a toon. Stay logged in-game, accept the Companion prompt, and try again.",
        >= HttpStatusCode.InternalServerError => "The Mingler is temporarily unavailable. Wait a minute, then run /register link again.",
        _ => "Registration did not complete. Run /register link again and retry."
    };
}
