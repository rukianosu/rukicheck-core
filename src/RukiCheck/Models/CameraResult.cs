using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// カメラ検査結果
/// </summary>
public class CameraResult
{
    [JsonPropertyName("captured")]
    public bool Captured { get; set; }

    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
