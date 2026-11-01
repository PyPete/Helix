using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Helix.Orchestration.Core.ScientificSetup;

public static class ScientificContentCanonicalizer
{
    public const string Schema = "scientific-setup/1";
    public static (string Content, string Hash) Canonicalize(JsonElement content, string schema = Schema)
    {
        if (schema != Schema) throw new ArgumentException("Unknown scientific schema.");
        var text = JsonSerializer.Serialize(new { content, schema });
        return (text, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant());
    }
}
