using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using MinglerSync.Core;

internal static class PresenceTests
{
    private static readonly DiscordPresenceConfiguration Configuration = new("123456789012345678", "mingler_sync");

    public static void Run(Action<string, Action> check)
    {
        check("presence preferences default off and fail closed", () => WithFile(path =>
        {
            var preferences = new PresencePreferences(path);
            Require(!preferences.LoadEnabled());
            foreach (var invalid in new[] { "{", "null", "[]", "{}", "{\"version\":2,\"discordPresenceEnabled\":true}",
                "{\"version\":1,\"discordPresenceEnabled\":\"true\"}", new string(' ', 1025) })
            {
                File.WriteAllText(path, invalid);
                Require(!preferences.LoadEnabled());
            }
        }));
        check("presence saves only explicit consent and remembers opt-out", () => WithFile(path =>
        {
            var preferences = new PresencePreferences(path);
            Require(preferences.TrySaveEnabled(true), "save opt-in");
            Require(preferences.LoadEnabled(), "load opt-in");
            using (var json = JsonDocument.Parse(File.ReadAllText(path)))
                Require(json.RootElement.EnumerateObject().Select(p => p.Name)
                    .Order().SequenceEqual(new[] { "discordPresenceEnabled", "version" }), "only consent fields");
            Require(preferences.TrySaveEnabled(false), "save opt-out");
            Require(!preferences.LoadEnabled(), "load opt-out");
        }));
        check("presence public configuration rejects missing and invalid values", () => WithFile(path =>
        {
            Require(!DiscordPresenceConfiguration.Load(path).IsConfigured);
            foreach (var invalid in new[] { "null", "{}", "{\"applicationId\":null,\"largeImageKey\":null}",
                "{\"applicationId\":1234}", "{", new string(' ', 4097) })
            {
                File.WriteAllText(path, invalid);
                Require(!DiscordPresenceConfiguration.Load(path).IsConfigured);
            }
            File.WriteAllText(path, JsonSerializer.Serialize(Configuration));
            Require(DiscordPresenceConfiguration.Load(path).IsConfigured);
            Require(!new DiscordPresenceConfiguration("bot-token", "mingler_sync").IsConfigured);
            Require(!new DiscordPresenceConfiguration(Configuration.ApplicationId, "https://example.com/icon").IsConfigured);
            Require(!new DiscordPresenceConfiguration(Configuration.ApplicationId + "\n", "mingler_sync").IsConfigured);
            Require(!new DiscordPresenceConfiguration(Configuration.ApplicationId, "mingler_sync\n").IsConfigured);
        }));
        check("presence payload is Playing with fixed icon and Unix session timer only", () =>
        {
            using var json = JsonSerializer.SerializeToDocument(DiscordPresenceClient.ActivityCommand(Configuration, 1700000000, "test"));
            var activity = json.RootElement.GetProperty("args").GetProperty("activity");
            Require(activity.GetProperty("type").GetInt32() == 0);
            Require(activity.GetProperty("timestamps").GetProperty("start").GetInt64() == 1700000000);
            Require(activity.GetProperty("assets").GetProperty("large_image").GetString() == "mingler_sync");
            Require(activity.GetProperty("assets").GetProperty("large_text").GetString() == "Mingler Sync");
            Require(activity.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "assets", "timestamps", "type" }));
        });
        check("disabled or unconfigured presence never opens IPC", () => NoConnectionAsync().GetAwaiter().GetResult());
        check("IPC handshake, fragmented frames, ping, publish acknowledgement and opt-out clear", () => LifecycleAsync().GetAwaiter().GetResult());
        check("Discord reconnect retains the original session timer", () => ReconnectAsync().GetAwaiter().GetResult());
        check("Discord rejection is not reported as an active presence", () => RejectionAsync().GetAwaiter().GetResult());
        check("oversized Discord frames are rejected safely", () => OversizedAsync().GetAwaiter().GetResult());
        check("opting out during handshake cannot publish a late activity", () => CancelHandshakeAsync().GetAwaiter().GetResult());
        check("unavailable Discord retries stop after opting out", () => StopRetriesAsync().GetAwaiter().GetResult());
    }

    private static async Task NoConnectionAsync()
    {
        var calls = 0;
        Task<Stream> Connect(CancellationToken token) { calls++; throw new IOException("must not connect"); }
        await using (var client = new DiscordPresenceClient(Configuration, Connect, TimeSpan.FromMilliseconds(10)))
        {
            await client.SetEnabledAsync(false);
            Require(calls == 0);
        }
        await using (var client = new DiscordPresenceClient(new("", "mingler_sync"), Connect, TimeSpan.FromMilliseconds(10)))
        {
            await client.SetEnabledAsync(true);
            Require(calls == 0);
        }
        Require(calls == 0);
    }

    private static async Task LifecycleAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        var name = "mingler-presence-test-" + Guid.NewGuid().ToString("N");
        using var server = Server(name);
        var calls = 0;
        await using var client = new DiscordPresenceClient(Configuration, async ct =>
        {
            Interlocked.Increment(ref calls);
            return await ConnectAsync(name, ct);
        }, TimeSpan.FromSeconds(15));
        var active = Signal();
        var statuses = new List<PresenceStatus>();
        client.StatusChanged += status => { lock (statuses) statuses.Add(status); if (status == PresenceStatus.Active) active.TrySetResult(); };
        await client.SetEnabledAsync(true);
        await server.WaitForConnectionAsync(token);
        using (var handshake = await ReceiveJsonAsync(server, 0, token))
        {
            Require(handshake.RootElement.GetProperty("v").GetInt32() == 1);
            Require(handshake.RootElement.GetProperty("client_id").GetString() == Configuration.ApplicationId);
        }
        await SendAsync(server, 3, Encoding.UTF8.GetBytes("ping"), token);
        var pong = await ReceiveAsync(server, token);
        Require(pong.Opcode == 4 && Encoding.UTF8.GetString(pong.Payload) == "ping");
        await SendAsync(server, 1, Encoding.UTF8.GetBytes("{\"cmd\":\"DISPATCH\",\"evt\":\"READY\",\"data\":{}}"), token, fragmented: true);
        using var activity = await ReceiveJsonAsync(server, 1, token);
        var nonce = activity.RootElement.GetProperty("nonce").GetString()!;
        await AckAsync(server, "wrong-nonce", token);
        Require(!active.Task.IsCompleted);
        await AckAsync(server, nonce, token);
        await active.Task.WaitAsync(token);
        var stopping = client.SetEnabledAsync(false);
        await ReceiveClearAsync(server, token);
        await stopping.WaitAsync(token);
        Require(calls == 1);
        lock (statuses) Require(statuses[^1] == PresenceStatus.Disabled);
    }

    private static async Task ReconnectAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        var name = "mingler-presence-test-" + Guid.NewGuid().ToString("N");
        using var first = Server(name);
        await using var client = new DiscordPresenceClient(Configuration, ct => ConnectAsync(name, ct), TimeSpan.FromMilliseconds(50));
        await client.SetEnabledAsync(true);
        var initial = await AcceptActivityAsync(first, token);
        await Task.Delay(1100, token);
        first.Dispose();
        using var second = Server(name);
        var reconnected = await AcceptActivityAsync(second, token);
        Require(initial == reconnected);
        // Closing the app clears presence too, without erasing the saved opt-in preference.
        var stopping = client.DisposeAsync().AsTask();
        await ReceiveClearAsync(second, token);
        await stopping.WaitAsync(token);
    }

    private static async Task RejectionAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        var name = "mingler-presence-test-" + Guid.NewGuid().ToString("N");
        using var server = Server(name);
        await using var client = new DiscordPresenceClient(Configuration, ct => ConnectAsync(name, ct), TimeSpan.FromSeconds(15));
        var active = false;
        client.StatusChanged += status => { if (status == PresenceStatus.Active) active = true; };
        await client.SetEnabledAsync(true);
        await server.WaitForConnectionAsync(token);
        using var handshake = await ReceiveJsonAsync(server, 0, token);
        await ReadyAsync(server, token);
        using var activity = await ReceiveJsonAsync(server, 1, token);
        await SendJsonAsync(server, new { cmd = "SET_ACTIVITY", evt = "ERROR", nonce = activity.RootElement.GetProperty("nonce").GetString() }, token);
        await ReceiveClearAsync(server, token);
        await client.SetEnabledAsync(false).WaitAsync(token);
        Require(!active);
    }

    private static async Task OversizedAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        var name = "mingler-presence-test-" + Guid.NewGuid().ToString("N");
        using var server = Server(name);
        await using var client = new DiscordPresenceClient(Configuration, ct => ConnectAsync(name, ct), TimeSpan.FromSeconds(15));
        var waiting = Signal();
        var active = false;
        var waitingCount = 0;
        client.StatusChanged += status =>
        {
            if (status == PresenceStatus.Active) active = true;
            if (status == PresenceStatus.WaitingForDiscord && Interlocked.Increment(ref waitingCount) == 2) waiting.TrySetResult();
        };
        await client.SetEnabledAsync(true);
        await server.WaitForConnectionAsync(token);
        using var handshake = await ReceiveJsonAsync(server, 0, token);
        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header, 1);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 65537);
        await server.WriteAsync(header, token);
        await waiting.Task.WaitAsync(token);
        await client.SetEnabledAsync(false).WaitAsync(token);
        Require(!active);
    }

    private static async Task CancelHandshakeAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        var name = "mingler-presence-test-" + Guid.NewGuid().ToString("N");
        using var server = Server(name);
        await using var client = new DiscordPresenceClient(Configuration, ct => ConnectAsync(name, ct), TimeSpan.FromSeconds(15));
        var active = false;
        client.StatusChanged += status => { if (status == PresenceStatus.Active) active = true; };
        await client.SetEnabledAsync(true);
        await server.WaitForConnectionAsync(token);
        using var handshake = await ReceiveJsonAsync(server, 0, token);
        await client.SetEnabledAsync(false).WaitAsync(token);
        var remaining = new byte[8];
        Require(await server.ReadAsync(remaining, token) == 0);
        Require(!active);
    }

    private static async Task StopRetriesAsync()
    {
        var attempted = Signal();
        var calls = 0;
        Task<Stream> Connect(CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            attempted.TrySetResult();
            throw new IOException("simulated Discord unavailable");
        }
        await using var client = new DiscordPresenceClient(Configuration, Connect, TimeSpan.FromMilliseconds(20));
        await client.SetEnabledAsync(true);
        await attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await client.SetEnabledAsync(false);
        var stoppedCalls = calls;
        await Task.Delay(100);
        Require(calls == stoppedCalls);
    }

    private static async Task<long> AcceptActivityAsync(NamedPipeServerStream server, CancellationToken token)
    {
        await server.WaitForConnectionAsync(token);
        using var handshake = await ReceiveJsonAsync(server, 0, token);
        await ReadyAsync(server, token);
        using var activity = await ReceiveJsonAsync(server, 1, token);
        await AckAsync(server, activity.RootElement.GetProperty("nonce").GetString()!, token);
        return activity.RootElement.GetProperty("args").GetProperty("activity").GetProperty("timestamps").GetProperty("start").GetInt64();
    }

    private static async Task ReceiveClearAsync(Stream server, CancellationToken token)
    {
        using var clear = await ReceiveJsonAsync(server, 1, token);
        Require(clear.RootElement.GetProperty("cmd").GetString() == "SET_ACTIVITY");
        Require(clear.RootElement.GetProperty("args").GetProperty("activity").ValueKind == JsonValueKind.Null);
        await AckAsync(server, clear.RootElement.GetProperty("nonce").GetString()!, token);
    }

    private static NamedPipeServerStream Server(string name) => new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
    private static async Task<Stream> ConnectAsync(string name, CancellationToken token)
    {
        var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        try { await pipe.ConnectAsync(token); return pipe; }
        catch { pipe.Dispose(); throw; }
    }
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ReadyAsync(Stream server, CancellationToken token) => SendJsonAsync(server, new { cmd = "DISPATCH", evt = "READY" }, token);
    private static Task AckAsync(Stream server, string nonce, CancellationToken token) => SendJsonAsync(server, new { cmd = "SET_ACTIVITY", nonce }, token);
    private static Task SendJsonAsync(Stream server, object message, CancellationToken token) => SendAsync(server, 1, JsonSerializer.SerializeToUtf8Bytes(message), token);
    private static async Task SendAsync(Stream server, int opcode, byte[] payload, CancellationToken token, bool fragmented = false)
    {
        var frame = new byte[payload.Length + 8];
        BinaryPrimitives.WriteInt32LittleEndian(frame, opcode);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), payload.Length);
        payload.CopyTo(frame, 8);
        if (fragmented)
        {
            await server.WriteAsync(frame.AsMemory(0, 3), token);
            await Task.Delay(20, token);
            await server.WriteAsync(frame.AsMemory(3), token);
        }
        else await server.WriteAsync(frame, token);
    }
    private static async Task<(int Opcode, byte[] Payload)> ReceiveAsync(Stream server, CancellationToken token)
    {
        var header = new byte[8];
        await server.ReadExactlyAsync(header, token);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
        Require(length is >= 0 and <= 65536);
        var payload = new byte[length];
        await server.ReadExactlyAsync(payload, token);
        return (BinaryPrimitives.ReadInt32LittleEndian(header), payload);
    }
    private static async Task<JsonDocument> ReceiveJsonAsync(Stream server, int opcode, CancellationToken token)
    {
        var frame = await ReceiveAsync(server, token);
        Require(frame.Opcode == opcode);
        return JsonDocument.Parse(frame.Payload);
    }
    private static void WithFile(Action<string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MinglerSync-presence-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        try { test(path); }
        finally { File.Delete(path); Directory.Delete(directory); }
    }
    private static void Require(bool condition, string? message = null)
    {
        if (!condition) throw new InvalidOperationException("presence assertion failed" + (message is null ? "" : ": " + message));
    }
}
