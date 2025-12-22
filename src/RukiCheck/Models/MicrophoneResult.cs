using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// マイク検査結果
/// </summary>
public class MicrophoneResult
{
    [JsonPropertyName("recorded")]
    public bool Recorded { get; set; }

    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("user_confirmed")]
    public bool UserConfirmed { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
