using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// Wi-Fi検査結果
/// </summary>
public class WiFiResult
{
    /// <summary>
    /// Wi-Fiアダプターが検出されたか
    /// </summary>
    [JsonPropertyName("adapter_found")]
    public bool AdapterFound { get; set; }

    /// <summary>
    /// アダプター名
    /// </summary>
    [JsonPropertyName("adapter_name")]
    public string AdapterName { get; set; } = string.Empty;

    /// <summary>
    /// アダプターの状態（有効/無効）
    /// </summary>
    [JsonPropertyName("adapter_status")]
    public string AdapterStatus { get; set; } = string.Empty;

    /// <summary>
    /// スキャンが成功したか
    /// </summary>
    [JsonPropertyName("scan_successful")]
    public bool ScanSuccessful { get; set; }

    /// <summary>
    /// 検出されたネットワーク数
    /// </summary>
    [JsonPropertyName("networks_found")]
    public int NetworksFound { get; set; }

    /// <summary>
    /// 検出されたネットワークのリスト
    /// </summary>
    [JsonPropertyName("available_networks")]
    public List<WiFiNetwork> AvailableNetworks { get; set; } = new();

    /// <summary>
    /// 現在接続中のネットワーク情報
    /// </summary>
    [JsonPropertyName("connected_network")]
    public WiFiNetwork? ConnectedNetwork { get; set; }

    /// <summary>
    /// ユーザーが電波受信を確認したか
    /// </summary>
    [JsonPropertyName("user_confirmed")]
    public bool UserConfirmed { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Wi-Fiネットワーク情報
/// </summary>
public class WiFiNetwork
{
    /// <summary>
    /// SSID（ネットワーク名）
    /// </summary>
    [JsonPropertyName("ssid")]
    public string Ssid { get; set; } = string.Empty;

    /// <summary>
    /// 信号強度（0-100%）
    /// </summary>
    [JsonPropertyName("signal_strength")]
    public int SignalStrength { get; set; }

    /// <summary>
    /// セキュリティタイプ（WPA2, WPA3, Open など）
    /// </summary>
    [JsonPropertyName("security_type")]
    public string SecurityType { get; set; } = string.Empty;

    /// <summary>
    /// 接続中かどうか
    /// </summary>
    [JsonPropertyName("is_connected")]
    public bool IsConnected { get; set; }

    /// <summary>
    /// 周波数帯（2.4GHz, 5GHz）
    /// </summary>
    [JsonPropertyName("frequency_band")]
    public string FrequencyBand { get; set; } = string.Empty;
}
