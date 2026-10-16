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
    private const int MaxConfirmationBytes = 1024 * 1024;
    private const string UnconfirmedMessage = "The server response could not confirm registration. Check /register status before trying again.";

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
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new RegisterSyncResult(false, GetFriendlyFailure(response.StatusCode));
            }

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentType?.MediaType is not { } mediaType
                || !(mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                    || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
                || response.Content.Headers.ContentLength > MaxConfirmationBytes)
                return Unconfirmed();

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await body.ReadAsync(chunk, timeout.Token)) != 0)
            {
                if (buffer.Length + read > MaxConfirmationBytes) return Unconfirmed();
                buffer.Write(chunk, 0, read);
            }
            return ParseSuccess(buffer.ToArray());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RegisterSyncResult(false, "The upload timed out. Check /register status before getting a new pairing link or trying again.");
        }
        catch (HttpRequestException)
        {
            return new RegisterSyncResult(false, "The connection could not confirm registration. Check your connection and /register status before trying again.");
        }
        catch (IOException)
        {
            return Unconfirmed();
        }
    }

    private static RegisterSyncResult Unconfirmed() => new(false, UnconfirmedMessage);

    private static RegisterSyncResult ParseSuccess(byte[] body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True
                || !root.TryGetProperty("syncedToons", out var countNode) || countNode.ValueKind != JsonValueKind.Number
                || !countNode.TryGetInt32(out var count) || count < 0
                || !root.TryGetProperty("toons", out var toonArray) || toonArray.ValueKind != JsonValueKind.Array
                || toonArray.GetArrayLength() != count)
                return Unconfirmed();

            var toons = new List<ToonSyncResult>();
            foreach (var toon in toonArray.EnumerateArray())
            {
                if (toon.ValueKind != JsonValueKind.Object
                    || !toon.TryGetProperty("name", out var nameNode) || nameNode.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(nameNode.GetString())
                    || !toon.TryGetProperty("wasNew", out var newNode)
                    || newNode.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                    || !toon.TryGetProperty("updated", out var updatedNode) || updatedNode.ValueKind != JsonValueKind.Array
                    || updatedNode.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
                    return Unconfirmed();
                toons.Add(new ToonSyncResult(nameNode.GetString()!, newNode.GetBoolean(),
                    updatedNode.EnumerateArray().Select(item => item.GetString()!).ToArray()));
            }

            // Only trusted confirmation fields control success; arbitrary server copy is not UI text.
            return new RegisterSyncResult(true, $"Registered {count} toon(s).", count, toons);
        }
        catch (JsonException)
        {
            return Unconfirmed();
        }
    }

    private static string GetFriendlyFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "That pairing link expired or was already used. Run /register link again and paste the new command.",
        HttpStatusCode.Forbidden => "The server rejected the connection. Check your connection settings or contact The Mingler staff.",
        HttpStatusCode.RequestEntityTooLarge => "The toon data was too large. Try again with fewer game accounts open, or contact The Mingler staff.",
        HttpStatusCode.UnprocessableEntity => "The server could not complete the import. Check /register status before getting a new pairing link or trying again.",
        >= HttpStatusCode.InternalServerError => "The Mingler could not confirm the import. Check /register status before getting a new pairing link or trying again.",
        _ => "Registration did not complete. Run /register link again and retry."
    };
}
