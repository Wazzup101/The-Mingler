using MinglerSync.Core;
using System.Text.Json;

var failures = new List<string>();

Check("parses current Discord command", () =>
{
    var input = @".\register-sync.cmd -SyncUrl ""https://sync.mingler.cc/api/register/sync"" -Token ""secret-value""";
    Require(PairingCommandParser.TryParse(input, out var pairing, out _));
    Require(pairing?.SyncUri.Host == "sync.mingler.cc");
    Require(pairing?.Token == "secret-value");
});

Check("accepts private LAN HTTP", () =>
{
    var input = @".\register-sync.cmd -SyncUrl ""http://192.168.50.10:3847/api/register/sync"" -Token ""abc""";
    Require(PairingCommandParser.TryParse(input, out _, out _));
});

Check("rejects public HTTP", () =>
{
    var input = @".\register-sync.cmd -SyncUrl ""http://example.com/api/register/sync"" -Token ""abc""";
    Require(!PairingCommandParser.TryParse(input, out _, out var error));
    Require(error.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
});

Check("rejects an unexpected upload path", () =>
{
    var input = @".\register-sync.cmd -SyncUrl ""https://sync.mingler.cc/not-register"" -Token ""abc""";
    Require(!PairingCommandParser.TryParse(input, out _, out _));
});

Check("rejects unexpected URL parameters", () =>
{
    var input = @".\register-sync.cmd -SyncUrl ""https://sync.mingler.cc/api/register/sync?forward=elsewhere"" -Token ""abc""";
    Require(!PairingCommandParser.TryParse(input, out _, out _));
});

Check("rejects missing pairing token", () =>
{
    Require(!PairingCommandParser.TryParse("-SyncUrl https://sync.mingler.cc/api/register/sync", out _, out _));
});

Check("prefers the toon name in Companion data", () =>
{
    using var document = JsonDocument.Parse("""{"name":"Account label","toon":{"name":"Flippy"}}""");
    Require(CompanionClient.FindToonLabel(document.RootElement) == "Flippy");
});

Check("scans sixteen local Companion ports", () =>
{
    Require(CompanionClient.FirstPort == 1547);
    Require(CompanionClient.LastPort == 1562);
    Require(CompanionClient.LastPort - CompanionClient.FirstPort + 1 == 16);
});

Check("certification demo uses fictional local profiles", () =>
{
    var profiles = CertificationDemo.CreateProfiles();
    Require(profiles.Count == 2);
    Require(profiles.All(profile => profile.Port == 0));
    Require(profiles.All(profile => profile.Payload.GetProperty("certificationDemo").GetBoolean()));
});

Check("rejects an unexpectedly large pasted command", () =>
{
    Require(!PairingCommandParser.TryParse(new string('x', 9000), out _, out _));
});

foreach (var failure in failures)
{
    Console.Error.WriteLine($"FAIL: {failure}");
}

Console.WriteLine(failures.Count == 0 ? "All Mingler Sync core checks passed." : $"{failures.Count} check(s) failed.");
return failures.Count == 0 ? 0 : 1;

void Check(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name} — {exception.Message}");
    }
}

static void Require(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("assertion failed");
    }
}
