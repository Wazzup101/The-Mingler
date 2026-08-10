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
            error = "That pairing command is unexpectedly long. Run /register link again.";
            return false;
        }

        var url = GetArgument(input, SyncUrlPattern());
        var token = GetArgument(input, TokenPattern());
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
        {
            error = "Copy the entire private command block from /register link.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            !SyncEndpointPolicy.TryValidate(uri, out error))
        {
            return false;
        }

        pairing = new PairingDetails(uri, token.Trim());
        return true;
    }

    private static string? GetArgument(string input, Regex pattern)
    {
        var match = pattern.Match(input);
        return match.Success
            ? match.Groups.Cast<Group>().Skip(1).FirstOrDefault(group => group.Success)?.Value
            : null;
    }

    [GeneratedRegex("""-SyncUrl\s+(?:"([^"]+)"|'([^']+)'|(\S+))""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SyncUrlPattern();

    [GeneratedRegex("""-Token\s+(?:"([^"]+)"|'([^']+)'|(\S+))""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}

