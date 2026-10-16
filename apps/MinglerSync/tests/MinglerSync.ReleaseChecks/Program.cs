using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MinglerSync.Core;

// Isolated release qualification. Synthetic data, unique loopback ports, no real
// pairing tokens, Companion sessions, production API, DB or Discord connection.
var checks = new List<object>();
var failures = 0;
using var profileDocument = JsonDocument.Parse("""{"toon":{"name":"Fixture Toon"}}""");
var profiles = new[] { new CompanionProfile(0, "Fixture Toon", profileDocument.RootElement.Clone()) };
const string FakeToken = "synthetic-release-check-token";

await Check("empty review cannot upload", async () =>
{
    var result = await new RegisterSyncClient().UploadAsync(new(new Uri("http://127.0.0.1:1/api/register/sync"), FakeToken), []);
    Require(!result.IsSuccess, "Empty review was accepted.");
});
await Check("valid server success and request contract", async () =>
{
    var (result, request) = await Exchange(200,
        """{"ok":true,"syncedToons":1,"message":"Registered one Toon.","toons":[{"name":"Fixture Toon","wasNew":true,"updated":["gags"]}]}""");
    Require(result.IsSuccess && result.SyncedToons == 1 && result.Toons?.Count == 1, "Valid success was not parsed.");
    Require(request.Method == "POST /api/register/sync HTTP/1.1", "Unexpected upload method or route.");
    Require(request.Headers["Authorization"] == "Bearer " + FakeToken, "Bearer authentication missing.");
    using var body = JsonDocument.Parse(request.Body);
    Require(body.RootElement.GetProperty("companions").GetArrayLength() == 1, "Wrong Companion envelope.");
    Require(!request.Body.Contains(FakeToken, StringComparison.Ordinal), "Pairing token entered the JSON body.");
});
foreach (var status in new[] { 302, 400, 401, 403, 413, 422, 429, 500, 503 })
{
    await Check($"HTTP {status} fails safely", async () =>
    {
        var (result, _) = await Exchange(status, """{"error":"synthetic-private-server-error"}""");
        Require(!result.IsSuccess, "Failed HTTP status was accepted.");
        Require(!result.Message.Contains("synthetic-private", StringComparison.Ordinal), "Raw server error entered UI copy.");
    });
}
foreach (var response in new[]
{
    ("HTML with HTTP 200", "<html>Unavailable</html>"),
    ("empty JSON object", "{}"),
    ("explicit failure with HTTP 200", """{"ok":false,"syncedToons":1}"""),
    ("negative confirmed count", """{"ok":true,"syncedToons":-1}"""),
    ("wrong count type", """{"ok":true,"syncedToons":"one"}"""),
    ("null root", "null"),
    ("array root", "[]"),
    ("nonboolean ok", """{"ok":"true","syncedToons":0,"toons":[]}"""),
    ("missing Toon list", """{"ok":true,"syncedToons":0}"""),
    ("count/list mismatch", """{"ok":true,"syncedToons":1,"toons":[]}"""),
    ("overflow count", """{"ok":true,"syncedToons":2147483648,"toons":[]}"""),
    ("fractional count", """{"ok":true,"syncedToons":0.5,"toons":[]}"""),
    ("wrong Toon shape", """{"ok":true,"syncedToons":1,"toons":[null]}"""),
    ("blank Toon name", """{"ok":true,"syncedToons":1,"toons":[{"name":" ","wasNew":true,"updated":[]}]}"""),
    ("wrong wasNew type", """{"ok":true,"syncedToons":1,"toons":[{"name":"Fixture","wasNew":"true","updated":[]}]}"""),
    ("wrong updated entry", """{"ok":true,"syncedToons":1,"toons":[{"name":"Fixture","wasNew":false,"updated":[17]}]}"""),
    ("oversized JSON body", new string(' ', 1024 * 1024 + 1)),
})
{
    await Check($"rejects invalid success: {response.Item1}", async () =>
    {
        var (result, _) = await Exchange(200, response.Item2);
        Require(!result.IsSuccess, "Invalid server confirmation was reported as a successful registration.");
    });
}
await Check("HTTP 204 is not registration confirmation", async () =>
{
    var (result, _) = await Exchange(204, "");
    Require(!result.IsSuccess, "Missing server confirmation was reported as a successful registration.");
});
await Check("explicit zero is not inflated from submitted profiles", async () =>
{
    var (result, _) = await Exchange(200, """{"ok":true,"syncedToons":0,"toons":[],"message":"synthetic-private-text"}""");
    Require(result.IsSuccess && result.SyncedToons == 0 && result.Toons?.Count == 0, "Confirmed zero was replaced by submitted count.");
    Require(!result.Message.Contains("synthetic-private"), "Untrusted server copy entered UI text.");
});
await Check("valid JSON with HTML media type is rejected", async () =>
{
    var (result, _) = await Exchange(200, """{"ok":true,"syncedToons":0,"toons":[]}""", "text/html");
    Require(!result.IsSuccess, "Unexpected media type was accepted.");
});
await Check("JSON structured media type is supported", async () =>
{
    var (result, _) = await Exchange(200, """{"ok":true,"syncedToons":0,"toons":[]}""", "application/vnd.mingler+json");
    Require(result.IsSuccess, "Structured JSON media type was rejected.");
});

Console.WriteLine(JsonSerializer.Serialize(new { total = checks.Count, passed = checks.Count - failures, failed = failures, checks }));
return failures == 0 ? 0 : 1;

async Task Check(string name, Func<Task> action)
{
    try { await action(); checks.Add(new { name, status = "PASS" }); Console.WriteLine("PASS: " + name); }
    catch (Exception error)
    {
        failures++;
        var reason = error is InvalidDataException ? error.Message : error.GetType().Name;
        checks.Add(new { name, status = "FAIL", reason }); Console.WriteLine("FAIL: " + name + " — " + reason);
    }
}

async Task<(RegisterSyncResult Result, CapturedRequest Request)> Exchange(int status, string responseBody, string contentType = "application/json")
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var token = timeout.Token;
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var response = Serve();
    var upload = new RegisterSyncClient().UploadAsync(new(new Uri($"http://127.0.0.1:{port}/api/register/sync"), FakeToken), profiles, token);
    await Task.WhenAll(response, upload);
    return (await upload, await response);

    async Task<CapturedRequest> Serve()
    {
        using var connection = await listener.AcceptTcpClientAsync(token);
        await using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
        var method = await reader.ReadLineAsync(token) ?? "";
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadLineAsync(token) is { Length: > 0 } line)
        {
            if (headers.Count > 32 || line.Length > 8192) throw new InvalidDataException("Oversized synthetic request headers.");
            var split = line.IndexOf(':');
            if (split > 0) headers[line[..split]] = line[(split + 1)..].Trim();
        }
        var body = new StringBuilder();
        if (headers.TryGetValue("Transfer-Encoding", out var transfer) && transfer.Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            while (true)
            {
                var chunk = await reader.ReadLineAsync(token) ?? throw new EndOfStreamException();
                var size = int.Parse(chunk.Split(';')[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (size == 0) { await reader.ReadLineAsync(token); break; }
                body.Append(await ReadChars(size));
                await reader.ReadLineAsync(token);
            }
        }
        else if (headers.TryGetValue("Content-Length", out var length)) body.Append(await ReadChars(int.Parse(length, CultureInfo.InvariantCulture)));
        var payload = Encoding.UTF8.GetBytes(responseBody);
        var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Test\r\nContent-Type: {contentType}\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head, token);
        await stream.WriteAsync(payload, token);
        await stream.FlushAsync(token);
        return new(method, headers, body.ToString());

        async Task<string> ReadChars(int size)
        {
            if (size < 0 || size > 65536) throw new InvalidDataException("Oversized synthetic request body.");
            var buffer = new char[size];
            if (await reader.ReadBlockAsync(buffer.AsMemory(), token) != size) throw new EndOfStreamException();
            return new string(buffer);
        }
    }
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
internal sealed record CapturedRequest(string Method, Dictionary<string, string> Headers, string Body);
