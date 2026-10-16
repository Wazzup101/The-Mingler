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
            request.Content = JsonContent.Create(CreateDiagnostic(phase, exception));

            using var response = await client.SendAsync(request, timeout.Token);
        }
        catch
        {
            // Diagnostic delivery must never interrupt registration or create a new user-facing error.
        }
    }

    // Arbitrary exception text can contain secrets, URLs, names or paths. Do not
    // try to redact it; send a fixed category and known operation phase only.
    internal static object CreateDiagnostic(string phase, Exception exception) => new
    {
        phase = phase is "companion_scan" or "sync_upload" ? phase : "unknown",
        code = exception switch
        {
            System.Text.Json.JsonException => "InvalidJson",
            HttpRequestException => "NetworkError",
            OperationCanceledException => "Cancelled",
            IOException => "InputOutputError",
            _ => "UnexpectedError",
        },
        message = "Unexpected client fault; raw error text omitted for privacy.",
        appVersion = AppRelease.Version,
    };
}
