using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

namespace MinglerSync.Core;

public enum PresenceStatus { Disabled, NotConfigured, WaitingForDiscord, Active }

/// <summary>
/// Opt-in local Discord IPC only. No account tokens, HTTP, game data or subscriptions.
/// One worker owns each connection, including cleanup, so disabling cannot race a late publish.
/// </summary>
public sealed class DiscordPresenceClient : IAsyncDisposable
{
    private readonly DiscordPresenceConfiguration _configuration;
    private readonly Func<CancellationToken, Task<Stream>> _connect;
    private readonly TimeSpan _retryDelay;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _stop;
    private Task? _worker;
    private bool _disposed;

    public event Action<PresenceStatus>? StatusChanged;

    public DiscordPresenceClient(DiscordPresenceConfiguration configuration)
        : this(configuration, ConnectDiscordAsync, TimeSpan.FromSeconds(15)) { }

    internal DiscordPresenceClient(DiscordPresenceConfiguration configuration,
        Func<CancellationToken, Task<Stream>> connect, TimeSpan retryDelay)
    {
        _configuration = configuration;
        _connect = connect;
        _retryDelay = retryDelay;
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await StopWorkerAsync().ConfigureAwait(false);
            if (!enabled)
            {
                StatusChanged?.Invoke(PresenceStatus.Disabled);
                return;
            }
            if (!_configuration.IsConfigured)
            {
                StatusChanged?.Invoke(PresenceStatus.NotConfigured);
                return;
            }
            _stop = new CancellationTokenSource();
            var token = _stop.Token;
            var started = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _worker = Task.Run(() => RunAsync(started, token));
        }
        finally { _gate.Release(); }
    }

    private async Task StopWorkerAsync()
    {
        if (_stop is null) return;
        _stop.Cancel();
        if (_worker is not null) await _worker.ConfigureAwait(false);
        _stop.Dispose();
        _stop = null;
        _worker = null;
    }

    private async Task RunAsync(long started, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Stream? pipe = null;
            var ready = false;
            try
            {
                StatusChanged?.Invoke(PresenceStatus.WaitingForDiscord);
                pipe = await _connect(token).ConfigureAwait(false);
                using (var timeout = Deadline(token))
                {
                    await WriteJsonAsync(pipe, 0, new { v = 1, client_id = _configuration.ApplicationId }, timeout.Token).ConfigureAwait(false);
                    await WaitForAsync(pipe, null, timeout.Token).ConfigureAwait(false);
                }
                ready = true;
                using (var timeout = Deadline(token))
                {
                    var nonce = Guid.NewGuid().ToString("N");
                    await WriteJsonAsync(pipe, 1, ActivityCommand(_configuration, started, nonce), timeout.Token).ConfigureAwait(false);
                    await WaitForAsync(pipe, nonce, timeout.Token).ConfigureAwait(false);
                }
                StatusChanged?.Invoke(PresenceStatus.Active);
                while (!token.IsCancellationRequested)
                {
                    using var message = await ReadMessageAsync(pipe, token).ConfigureAwait(false);
                    RejectError(message.RootElement);
                }
            }
            catch (Exception error) when (error is IOException or TimeoutException or UnauthorizedAccessException
                or JsonException or OperationCanceledException)
            {
                // Discord can close, restart, reject an app ID, or be unavailable. Never log its payloads.
            }
            finally
            {
                if (pipe is not null)
                {
                    if (ready)
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
                        try
                        {
                            var nonce = Guid.NewGuid().ToString("N");
                            await WriteJsonAsync(pipe, 1, ClearCommand(nonce), timeout.Token).ConfigureAwait(false);
                            await WaitForAsync(pipe, nonce, timeout.Token).ConfigureAwait(false);
                        }
                        catch (Exception error) when (error is IOException or TimeoutException or JsonException
                            or OperationCanceledException or UnauthorizedAccessException) { }
                    }
                    pipe.Dispose();
                }
            }
            if (token.IsCancellationRequested) break;
            StatusChanged?.Invoke(PresenceStatus.WaitingForDiscord);
            try { await Task.Delay(_retryDelay, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    internal static object ActivityCommand(DiscordPresenceConfiguration configuration, long started, string nonce) => new
    {
        cmd = "SET_ACTIVITY",
        args = new
        {
            pid = Environment.ProcessId,
            activity = new
            {
                type = 0,
                timestamps = new { start = started },
                assets = new { large_image = configuration.LargeImageKey, large_text = "Mingler Sync" },
            },
        },
        nonce,
    };

    internal static object ClearCommand(string nonce) => new
    {
        cmd = "SET_ACTIVITY",
        args = new { pid = Environment.ProcessId, activity = (object?)null },
        nonce,
    };

    private static CancellationTokenSource Deadline(CancellationToken token)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        return timeout;
    }

    private static async Task<Stream> ConnectDiscordAsync(CancellationToken token)
    {
        for (var index = 0; index < 10; index++)
        {
            token.ThrowIfCancellationRequested();
            var pipe = new NamedPipeClientStream(".", $"discord-ipc-{index}", PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(150, token).ConfigureAwait(false);
                return pipe;
            }
            catch (Exception error) when (error is IOException or TimeoutException or UnauthorizedAccessException)
            {
                pipe.Dispose();
            }
            catch { pipe.Dispose(); throw; }
        }
        throw new IOException("Discord desktop is unavailable.");
    }

    private static async Task WaitForAsync(Stream pipe, string? nonce, CancellationToken token)
    {
        while (true)
        {
            using var message = await ReadMessageAsync(pipe, token).ConfigureAwait(false);
            var root = message.RootElement;
            RejectError(root);
            if (nonce is null && Text(root, "evt") == "READY" && Text(root, "cmd") == "DISPATCH") return;
            if (nonce is not null && Text(root, "nonce") == nonce && Text(root, "cmd") == "SET_ACTIVITY") return;
        }
    }

    private static string? Text(JsonElement root, string key) => root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static void RejectError(JsonElement root)
    {
        if (Text(root, "evt") == "ERROR") throw new IOException("Discord rejected the activity.");
    }

    private static async Task<JsonDocument> ReadMessageAsync(Stream pipe, CancellationToken token)
    {
        while (true)
        {
            var header = new byte[8];
            await pipe.ReadExactlyAsync(header, token).ConfigureAwait(false);
            var opcode = BinaryPrimitives.ReadInt32LittleEndian(header);
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
            if (length < 0 || length > 65536) throw new IOException("Invalid Discord frame size.");
            var payload = new byte[length];
            await pipe.ReadExactlyAsync(payload, token).ConfigureAwait(false);
            if (opcode == 3)
            {
                await WriteFrameAsync(pipe, 4, payload, token).ConfigureAwait(false);
                continue;
            }
            if (opcode == 4) continue;
            if (opcode != 1) throw new IOException("Discord closed or sent an unsupported frame.");
            return JsonDocument.Parse(payload);
        }
    }

    private static Task WriteJsonAsync(Stream pipe, int opcode, object payload, CancellationToken token) =>
        WriteFrameAsync(pipe, opcode, JsonSerializer.SerializeToUtf8Bytes(payload), token);

    private static async Task WriteFrameAsync(Stream pipe, int opcode, byte[] payload, CancellationToken token)
    {
        var frame = new byte[8 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, opcode);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), payload.Length);
        payload.CopyTo(frame, 8);
        await pipe.WriteAsync(frame, token).ConfigureAwait(false);
        await pipe.FlushAsync(token).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            await StopWorkerAsync().ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
