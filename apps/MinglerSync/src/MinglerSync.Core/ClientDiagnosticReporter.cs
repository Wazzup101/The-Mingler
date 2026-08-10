using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MinglerSync.Core;

public sealed class ClientDiagnosticReporter
{
    private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(8);

    public async Task TryReportUnexpectedAsync(
        PairingDetails pairing,
        string phase,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ReportTimeout);
            using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(pairing.SyncUri, "/api/register/client-error"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pairing.Token);
            request.Content = JsonContent.Create(new
            {
                phase = Normalize(phase, 32),
                code = Normalize(exception.GetType().Name, 64),
                message = NormalizeMessage(exception.Message),
                appVersion = "1.0.0",
            });

            using var response = await client.SendAsync(request, timeout.Token);
        }
        catch
        {
            // Diagnostic delivery must never interrupt registration or create a new user-facing error.
        }
    }

    private static string Normalize(string? value, int maximumLength)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static string NormalizeMessage(string? value)
    {
        var normalized = Normalize(value, 600);
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"(?i)\b[a-z]:\\[^\s]+",
            "[local path]");
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"\\\\[^\s]+",
            "[network path]");
        return Normalize(normalized, 300);
    }
}
