using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// トラックパッド検査結果
/// </summary>
public class TrackpadResult
{
    [JsonPropertyName("cursor_moved")]
    public bool CursorMoved { get; set; }

    [JsonPropertyName("left_click")]
    public bool LeftClick { get; set; }

    [JsonPropertyName("right_click")]
    public bool RightClick { get; set; }

    [JsonPropertyName("scroll_detected")]
    public bool ScrollDetected { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "unknown"; // pass / warn / fail

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
