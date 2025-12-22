using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// キーボード検査結果
/// </summary>
public class KeyboardResult
{
    [JsonPropertyName("layout")]
    public string Layout { get; set; } = "JIS"; // JIS / US

    [JsonPropertyName("total_keys")]
    public int TotalKeys { get; set; }

    [JsonPropertyName("pressed_keys")]
    public int PressedKeys { get; set; }

    [JsonPropertyName("missing_keys")]
    public List<string> MissingKeys { get; set; } = new();

    [JsonPropertyName("result")]
    public string Result { get; set; } = "unknown"; // pass / warn / fail

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
