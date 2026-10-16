using System.Text.Json;
using System.Text.RegularExpressions;

namespace MinglerSync.Core;

/// <summary>Public publisher configuration, never a bot token or client secret.</summary>
public sealed record DiscordPresenceConfiguration(string ApplicationId, string LargeImageKey)
{
    public bool IsConfigured => ApplicationId is not null && LargeImageKey is not null
        && Regex.IsMatch(ApplicationId, "\\A[0-9]{17,20}\\z")
        && ulong.TryParse(ApplicationId, out var id) && id > 0
        && Regex.IsMatch(LargeImageKey, "\\A[a-z0-9_]{1,32}\\z");

    public static DiscordPresenceConfiguration Load(string path)
    {
        try
        {
            if (new FileInfo(path).Length > 4096) return Empty;
            using var file = File.OpenRead(path);
            return JsonSerializer.Deserialize<DiscordPresenceConfiguration>(file,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) is { IsConfigured: true } value
                ? value : Empty;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return Empty;
        }
    }

    private static DiscordPresenceConfiguration Empty => new("", "mingler_sync");
}
