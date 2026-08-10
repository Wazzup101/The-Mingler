using MinglerSync.Core;
using System.Text.Json;

var failures = new List<string>();
Check("parses current command", () => Require(PairingCommandParser.TryParse(@".\register-sync.cmd -SyncUrl ""https://sync.mingler.cc/api/register/sync"" -Token ""secret""", out var pairing, out _) && pairing?.Token == "secret"));
Check("accepts private HTTP", () => Require(PairingCommandParser.TryParse(@"-SyncUrl ""http://192.168.1.2:3847/api/register/sync"" -Token ""x""", out _, out _)));
Check("rejects public HTTP", () => Require(!PairingCommandParser.TryParse(@"-SyncUrl ""http://example.com/api/register/sync"" -Token ""x""", out _, out _)));
Check("rejects unexpected path", () => Require(!PairingCommandParser.TryParse(@"-SyncUrl ""https://sync.mingler.cc/other"" -Token ""x""", out _, out _)));
Check("rejects URL parameters", () => Require(!PairingCommandParser.TryParse(@"-SyncUrl ""https://sync.mingler.cc/api/register/sync?x=1"" -Token ""x""", out _, out _)));
Check("rejects missing token", () => Require(!PairingCommandParser.TryParse("-SyncUrl https://sync.mingler.cc/api/register/sync", out _, out _)));
Check("reads toon name", () => { using var document = JsonDocument.Parse("""{"toon":{"name":"Flippy"}}"""); Require(CompanionClient.FindToonLabel(document.RootElement) == "Flippy"); });
Check("rejects oversized input", () => Require(!PairingCommandParser.TryParse(new string('x', 9000), out _, out _)));
foreach (var failure in failures) Console.Error.WriteLine($"FAIL: {failure}");
Console.WriteLine(failures.Count == 0 ? "All Mingler Sync core checks passed." : $"{failures.Count} check(s) failed.");
return failures.Count == 0 ? 0 : 1;
void Check(string name, Action test) { try { test(); Console.WriteLine($"PASS: {name}"); } catch (Exception exception) { failures.Add($"{name} - {exception.Message}"); } }
static void Require(bool condition) { if (!condition) throw new InvalidOperationException("assertion failed"); }

