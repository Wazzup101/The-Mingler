using System.Text.Json;

namespace MinglerSync.Core;

public static class CertificationDemo
{
    public static IReadOnlyList<CompanionProfile> CreateProfiles()
    {
        return
        [
            CreateProfile(0, "Sample Toon", "cat", 34),
            CreateProfile(0, "Demo Duck", "duck", 52),
        ];
    }

    private static CompanionProfile CreateProfile(int port, string name, string species, int laff)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            toon = new
            {
                name,
                species,
                laff,
            },
            certificationDemo = true,
        }));

        return new CompanionProfile(port, name, document.RootElement.Clone());
    }
}
