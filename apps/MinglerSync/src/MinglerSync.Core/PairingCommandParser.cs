using System.Text.RegularExpressions;

namespace MinglerSync.Core;

public sealed record PairingDetails(Uri SyncUri, string Token);

public static partial class PairingCommandParser
{
    public static bool TryParse(string? input, out PairingDetails? pairing, out string error)
    {
        pairing = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Paste the command from /register link first.";
            return false;
        }

        if (input.Length > 8192)
        {
            error = "That pairing command is unexpectedly long. Run /register link again and copy only the command block.";
            return false;
        }

        var url = GetArgument(input, SyncUrlPattern());
        var token = GetArgument(input, TokenPattern());

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
        {
            error = "That does not look like the full /register link command. Copy the entire command block from Discord.";
            return false;
        }

        if (url.Length > 2048 || token.Length > 4096)
        {
            error = "That pairing command is unexpectedly long. Run /register link again.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var syncUri))
        {
            error = "The registration address in that command is not valid. Run /register link again.";
            return false;
        }

        if (!SyncEndpointPolicy.TryValidate(syncUri, out error))
        {
            return false;
        }

        pairing = new PairingDetails(syncUri, token.Trim());
        return true;
    }

    private static string? GetArgument(string input, Regex pattern)
    {
        var match = pattern.Match(input);
        if (!match.Success)
        {
            return null;
        }

        for (var i = 1; i < match.Groups.Count; i++)
        {
            if (match.Groups[i].Success)
            {
                return match.Groups[i].Value;
            }
        }

        return null;
    }

    [GeneratedRegex("""-SyncUrl\s+(?:"([^"]+)"|'([^']+)'|(\S+))""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SyncUrlPattern();

    [GeneratedRegex("""-Token\s+(?:"([^"]+)"|'([^']+)'|(\S+))""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
