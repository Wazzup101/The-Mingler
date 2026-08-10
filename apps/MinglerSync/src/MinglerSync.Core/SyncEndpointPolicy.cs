using System.Net;

namespace MinglerSync.Core;

public static class SyncEndpointPolicy
{
    public static bool TryValidate(Uri uri, out string error)
    {
        error = string.Empty;

        if (!string.Equals(uri.AbsolutePath.TrimEnd('/'), "/api/register/sync", StringComparison.OrdinalIgnoreCase))
        {
            error = "The registration address has an unexpected path. Run /register link again instead of editing the command.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.Query))
        {
            error = "The registration address contains unsupported information. Run /register link again.";
            return false;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && IsPrivateHost(uri.Host))
        {
            return true;
        }

        error = "For your safety, internet registration must use HTTPS. Plain HTTP is accepted only for a private home-network address.";
        return false;
    }

    private static bool IsPrivateHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               (bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168));
    }
}
