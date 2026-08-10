using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MinglerSync.Core;

public sealed record RegisterSyncResult(bool IsSuccess, string Message, int SyncedToons = 0);

public sealed class RegisterSyncClient
{
    public async Task<RegisterSyncResult> UploadAsync(PairingDetails pairing, IReadOnlyList<CompanionProfile> profiles, CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0) return new(false, "No toon data is ready to send.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pairing.Token);
        using var response = await client.PostAsJsonAsync(pairing.SyncUri, new { companions = profiles.Select(profile => profile.Payload).ToArray() }, timeout.Token);
        if (response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
            return new(false, "The registration service returned an unexpected redirect. No data was resent.");

        if (!response.IsSuccessStatusCode)
            return new(false, response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? "The pairing link expired or was already used. Run /register link again."
                : $"Registration failed ({(int)response.StatusCode}). Try again later.");

        try
        {
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            var count = payload.RootElement.TryGetProperty("synced_toons", out var synced) && synced.TryGetInt32(out var parsed) ? parsed : profiles.Count;
            return new(true, $"Synced {count} toon(s).", count);
        }
        catch (JsonException)
        {
            return new(true, $"Synced {profiles.Count} toon(s).", profiles.Count);
        }
    }
}

