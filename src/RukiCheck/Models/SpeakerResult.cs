using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// スピーカー検査結果
/// </summary>
public class SpeakerResult
{
    [JsonPropertyName("left")]
    public bool Left { get; set; }

    [JsonPropertyName("right")]
    public bool Right { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
