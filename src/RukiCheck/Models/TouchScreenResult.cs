using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// タッチスクリーン検査結果
/// </summary>
public class TouchScreenResult
{
    /// <summary>
    /// タッチスクリーンが検出されたか
    /// </summary>
    [JsonPropertyName("touch_available")]
    public bool TouchAvailable { get; set; }

    /// <summary>
    /// デバイス名
    /// </summary>
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// タップ（タッチ）が検出されたか
    /// </summary>
    [JsonPropertyName("tap_detected")]
    public bool TapDetected { get; set; }

    /// <summary>
    /// マルチタッチ対応か
    /// </summary>
    [JsonPropertyName("multi_touch_supported")]
    public bool MultiTouchSupported { get; set; }

    /// <summary>
    /// 最大同時タッチポイント数
    /// </summary>
    [JsonPropertyName("max_touch_points")]
    public int MaxTouchPoints { get; set; }

    /// <summary>
    /// スワイプ操作が検出されたか
    /// </summary>
    [JsonPropertyName("swipe_detected")]
    public bool SwipeDetected { get; set; }

    /// <summary>
    /// テスト結果（pass / warn / fail / not_available）
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = "not_available";

    /// <summary>
    /// 注記
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
