using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// 検品レポート全体のデータモデル
/// </summary>
public class InspectionReport
{
    [JsonPropertyName("meta")]
    public InspectionMeta Meta { get; set; } = new();

    [JsonPropertyName("storage")]
    public StorageResult? Storage { get; set; }

    [JsonPropertyName("keyboard")]
    public KeyboardResult? Keyboard { get; set; }

    [JsonPropertyName("microphone")]
    public MicrophoneResult? Microphone { get; set; }

    [JsonPropertyName("speaker")]
    public SpeakerResult? Speaker { get; set; }

    [JsonPropertyName("camera")]
    public CameraResult? Camera { get; set; }

    [JsonPropertyName("cpu")]
    public CpuResult? Cpu { get; set; }
}
