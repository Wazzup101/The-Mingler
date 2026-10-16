using System.Text.Json;

namespace MinglerSync.Core;

/// <summary>Stores only explicit presence consent. Missing or invalid settings mean off.</summary>
public sealed class PresencePreferences(string path)
{
    public bool LoadEnabled()
    {
        try
        {
            if (new FileInfo(path).Length > 1024) return false;
            using var file = File.OpenRead(path);
            using var document = JsonDocument.Parse(file);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.Number
                && version.TryGetInt32(out var number) && number == 1
                && root.TryGetProperty("discordPresenceEnabled", out var enabled)
                && enabled.ValueKind == JsonValueKind.True;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public bool TrySaveEnabled(bool enabled)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { version = 1, discordPresenceEnabled = enabled }));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
